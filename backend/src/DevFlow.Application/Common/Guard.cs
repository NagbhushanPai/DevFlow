using DevFlow.Application.Common.Exceptions;
namespace DevFlow.Application.Common;

/// <summary>
/// Small input-normalisation helpers shared by the application services.
/// </summary>
public static class Guard
{
    public static string Required(string? value, string fieldName, int maxLength = int.MaxValue)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new BadRequestException($"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? Optional(string? value, string fieldName = "Value", int maxLength = int.MaxValue)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new BadRequestException($"{fieldName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static void ValidDateRange(DateTime? startUtc, DateTime? endUtc)
    {
        if (startUtc.HasValue && endUtc.HasValue && endUtc.Value < startUtc.Value)
        {
            throw new BadRequestException("The end date must be on or after the start date.");
        }
    }
}
