using System.Security.Claims;
using DevFlow.Domain.Entities;
using DevFlow.Infrastructure.Auditing;

namespace DevFlow.API.Middleware;

/// <summary>
/// Records every state-changing request. Entries are queued and written in the background, so this adds no
/// database work to the request and cannot affect the request's own DbContext.
/// </summary>
public sealed class AuditLoggingMiddleware(RequestDelegate next, AuditLogQueue queue, ILogger<AuditLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            if (ShouldAudit(context.Request))
            {
                Enqueue(context);
            }
        }
    }

    private static bool ShouldAudit(HttpRequest request) =>
        !(HttpMethods.IsGet(request.Method)
          || HttpMethods.IsHead(request.Method)
          || HttpMethods.IsOptions(request.Method)
          || request.Path.StartsWithSegments("/health")
          || request.Path.StartsWithSegments("/swagger")
          || request.Path.StartsWithSegments("/hubs"));

    private void Enqueue(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var entry = new AuditLog
        {
            UserId = Guid.TryParse(userId, out var parsed) ? parsed : null,
            Action = Truncate($"{context.Request.Method} {context.Request.Path}", 100),
            HttpMethod = context.Request.Method,
            Path = Truncate(context.Request.Path.ToString(), 500),
            StatusCode = context.Response.StatusCode,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 500),
            TraceId = context.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow,
        };

        if (!queue.TryEnqueue(entry))
        {
            logger.LogWarning("Audit queue is full; dropped entry for {Action}.", entry.Action);
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
