using System.Threading.Channels;
using DevFlow.Domain.Entities;
using DevFlow.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevFlow.Infrastructure.Auditing;

/// <summary>
/// Bounded queue of audit entries. Requests only enqueue; <see cref="AuditLogWriterService"/> persists them
/// in batches using its <b>own</b> DbContext, so audit writes can never flush a request's half-finished changes.
/// </summary>
public sealed class AuditLogQueue
{
    private readonly Channel<AuditLog> _channel = Channel.CreateBounded<AuditLog>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<AuditLog> Reader => _channel.Reader;

    public bool TryEnqueue(AuditLog entry) => _channel.Writer.TryWrite(entry);
}

public sealed class AuditLogWriterService(
    AuditLogQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditLogWriterService> logger) : BackgroundService
{
    private const int MaxBatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditLog>(MaxBatchSize);

        try
        {
            while (await queue.Reader.WaitToReadAsync(stoppingToken))
            {
                while (batch.Count < MaxBatchSize && queue.Reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }

                await WriteAsync(batch, stoppingToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Flush whatever is already queued before shutting down.
            while (queue.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            await WriteAsync(batch, CancellationToken.None);
        }
    }

    private async Task WriteAsync(List<AuditLog> batch, CancellationToken ct)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AuditLogs.AddRange(batch);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to write {Count} audit log entries.", batch.Count);
        }
    }
}
