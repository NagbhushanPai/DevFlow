using System.Security.Claims;
using System.Threading.RateLimiting;
using DevFlow.API.ExceptionHandling;
using DevFlow.API.Extensions;
using DevFlow.API.Filters;
using DevFlow.API.Hubs;
using DevFlow.API.Middleware;
using DevFlow.Application;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Infrastructure;
using DevFlow.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging is fully driven by the "Serilog" configuration section.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();

builder.Services.AddSwaggerDocumentation();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

builder.Services.AddDevFlowRateLimiting(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (forwardedHeadersEnabled)
{
    // Behind nginx / a load balancer: trust X-Forwarded-For/-Proto so client IPs (rate limiting, audit)
    // and HTTPS detection (Secure cookies) are correct. Only enable when the API is not directly exposed.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

// `dotnet DevFlow.API.dll --migrate` applies migrations + seeds once and exits. Use it as a one-shot
// job (see docker-compose "migrate" service) instead of migrating from every API replica on startup.
var migrateOnly = args.Contains("--migrate");
var applyMigrationsOnStartup = builder.Configuration.GetValue<bool>("Database:ApplyMigrations");
var seedDemoData = builder.Configuration.GetValue<bool>("Seed:DemoData");

if (migrateOnly)
{
    await DatabaseInitializer.InitializeAsync(app.Services, applyMigrations: true, seedDemoData);
    return;
}

await DatabaseInitializer.InitializeAsync(app.Services, applyMigrationsOnStartup, seedDemoData);

if (forwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Off in containers, where TLS is terminated by the reverse proxy and the app listens on HTTP only.
if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");

// Authentication must run before rate limiting and auditing so both can see the user.
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<IssueHub>("/hubs/issues");
app.MapHealthChecks("/health").DisableRateLimiting();

app.Run();

public partial class Program;

internal static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddDevFlowRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPermitLimit = configuration.GetValue("RateLimiting:PermitLimitPerMinute", 100);
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitLimitPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    title = "Too Many Requests",
                    detail = "Request rate limit exceeded. Please try again later.",
                }, cancellationToken);
            };

            // Authenticated users get their own bucket; anonymous traffic is bucketed by client IP.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            // Stricter per-IP limit for login/register/refresh to slow down credential stuffing.
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    $"auth:{httpContext.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static string PartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
