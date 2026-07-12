namespace DevFlow.Application.Common.Models;

public sealed record JwtTokenResult(
    string Token,
    DateTime ExpiresAtUtc);