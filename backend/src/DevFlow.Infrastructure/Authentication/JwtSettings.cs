using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevFlow.Infrastructure.Authentication;

public sealed class JwtSettings : IValidatableObject
{
    public const string SectionName = "JwtSettings";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinimumSecretBytes = 32;

    public string Secret { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Access-token lifetime. Kept short because refresh tokens handle long sessions.</summary>
    [Range(1, 1440)]
    public int ExpirationInMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpirationInDays { get; init; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Secret))
        {
            yield return new ValidationResult("JwtSettings:Secret is not configured.", [nameof(Secret)]);
        }
        else if (Encoding.UTF8.GetByteCount(Secret) < MinimumSecretBytes)
        {
            yield return new ValidationResult(
                $"JwtSettings:Secret must be at least {MinimumSecretBytes} bytes (got {Encoding.UTF8.GetByteCount(Secret)}).",
                [nameof(Secret)]);
        }
    }
}
