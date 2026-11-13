using System.Threading.Channels;
using DevFlow.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevFlow.Infrastructure.Email;

public sealed record EmailMessage(string ToEmail, string Subject, string Body);

/// <summary>Bounded in-memory queue of outgoing emails.</summary>
public sealed class EmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public bool TryEnqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
}

/// <summary>
/// The <see cref="IEmailNotificationSender"/> used by the application: it only enqueues, so HTTP requests never
/// wait on SMTP. <see cref="EmailDispatcherService"/> performs the actual delivery.
/// </summary>
public sealed class QueuedEmailSender(EmailQueue queue, ILogger<QueuedEmailSender> logger) : IEmailNotificationSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        if (!queue.TryEnqueue(new EmailMessage(toEmail, subject, body)))
        {
            logger.LogWarning("Email queue rejected a message to {ToEmail}.", toEmail);
        }

        return Task.CompletedTask;
    }
}

public sealed class EmailDispatcherService(
    EmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
                await sender.SendAsync(message.ToEmail, message.Subject, message.Body, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deliver email to {ToEmail}.", message.ToEmail);
            }
        }
    }
}
