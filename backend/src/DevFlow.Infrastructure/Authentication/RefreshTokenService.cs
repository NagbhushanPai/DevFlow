using System.Security.Cryptography;
using System.Text;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevFlow.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    IApplicationDbContext context,
    IOptions<JwtSettings> options,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public async Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct)
    {
        var (token, entity) = Create(userId);
        context.RefreshTokens.Add(entity);
        await context.SaveChangesAsync(ct);
        return token;
    }

    public async Task<RefreshTokenRotation?> RotateAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var existing = await context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (existing.RevokedAtUtc is not null)
        {
            // A rotated token was replayed: assume it was stolen and end every session of this user.
            logger.LogWarning("Revoked refresh token reused for user {UserId}; revoking all sessions.", existing.UserId);
            await RevokeAllAsync(existing.UserId, now, ct);
            return null;
        }

        if (existing.ExpiresAtUtc <= now)
        {
            return null;
        }

        existing.RevokedAtUtc = now;
        var (newToken, entity) = Create(existing.UserId);
        context.RefreshTokens.Add(entity);
        await context.SaveChangesAsync(ct);

        return new RefreshTokenRotation(existing.UserId, newToken);
    }

    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var existing = await context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAtUtc == null, ct);
        if (existing is null)
        {
            return;
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var active = await context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
        }

        await context.SaveChangesAsync(ct);
    }

    private (RefreshTokenResult Token, RefreshToken Entity) Create(Guid userId)
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = DateTime.UtcNow;
        var expires = now.AddDays(_settings.RefreshTokenExpirationInDays);

        var entity = new RefreshToken
        {
            TokenHash = Hash(value),
            UserId = userId,
            ExpiresAtUtc = expires,
            CreatedAtUtc = now,
        };

        return (new RefreshTokenResult(value, expires), entity);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
