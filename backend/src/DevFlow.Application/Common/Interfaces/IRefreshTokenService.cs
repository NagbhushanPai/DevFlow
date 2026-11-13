namespace DevFlow.Application.Common.Interfaces;

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);

public sealed record RefreshTokenRotation(Guid UserId, RefreshTokenResult NewToken);

/// <summary>
/// Issues and rotates opaque refresh tokens. Only a hash of each token is stored.
/// </summary>
public interface IRefreshTokenService
{
    Task<RefreshTokenResult> IssueAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Revokes <paramref name="token"/> and issues a replacement. Returns <c>null</c> if the token is unknown,
    /// expired or already revoked; presenting a revoked token is treated as theft and revokes all of the
    /// user's tokens.
    /// </summary>
    Task<RefreshTokenRotation?> RotateAsync(string token, CancellationToken ct);

    Task RevokeAsync(string token, CancellationToken ct);
}
