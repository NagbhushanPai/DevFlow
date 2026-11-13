using DevFlow.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.API.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException =>
                CreateValidationProblemDetails(
                    validationException),

            RegistrationException registrationException =>
                CreateRegistrationProblemDetails(
                    registrationException),

            InvalidCredentialsException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Authentication failed",
                    Detail = exception.Message
                },

            ForbiddenAccessException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = exception.Message
                },

            NotFoundException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not found",
                    Detail = exception.Message
                },

            UnauthorizedAccessException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication is required."
                },

            ConflictException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = exception.Message
                },

            DbUpdateConcurrencyException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = "The resource was modified by someone else. Reload and try again."
                },

            ExternalServiceException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = "External service unavailable",
                    Detail = exception.Message
                },

            BadHttpRequestException badRequest =>
                new ProblemDetails
                {
                    Status = badRequest.StatusCode,
                    Title = "Bad request",
                    Detail = exception.Message
                },

            BadRequestException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid request",
                    Detail = exception.Message
                },

            _ =>
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status500InternalServerError,
                    Title = "Server error",
                    Detail =
                        "An unexpected error occurred."
                }
        };

        httpContext.Response.StatusCode =
            problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        // Expected client errors are informational; only unexpected failures are logged as errors.
        if (httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            logger.LogInformation("Request failed with {StatusCode}: {Message}", httpContext.Response.StatusCode, exception.Message);
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        // Serialize using the runtime type so ValidationProblemDetails.Errors is included.
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            problemDetails.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateValidationProblemDetails(
        ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };
    }

    private static ProblemDetails CreateRegistrationProblemDetails(
        RegistrationException exception)
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Registration failed",
            Detail = exception.Message,
            Extensions =
            {
                ["errors"] = exception.Errors
            }
        };
    }
}
