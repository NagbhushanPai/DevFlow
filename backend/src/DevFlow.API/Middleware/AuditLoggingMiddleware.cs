using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;

namespace DevFlow.API.Middleware;

public sealed class AuditLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApplicationDbContext db)
    {
        await next(context);

        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            return;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? actorId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actorId,
            Action = $"{context.Request.Method} {context.Request.Path}",
            HttpMethod = context.Request.Method,
            Path = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            TraceId = context.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(context.RequestAborted);
    }
}
