using DevFlow.Application.Common.Interfaces;
using DevFlow.Infrastructure.Identity;
using DevFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using DevFlow.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using DevFlow.Infrastructure.Storage;
using DevFlow.Infrastructure.Email;
using DevFlow.Infrastructure.GitHub;


namespace DevFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
    "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(
                name: "database");

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.Configure<JwtSettings>(
    configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IIdentityService, IdentityService>();


        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration
                    .GetRequiredSection(JwtSettings.SectionName)
                    .Get<JwtSettings>()
                    ?? throw new InvalidOperationException("JWT settings were not configured.");

                if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
                {
                    throw new InvalidOperationException(
                        "JWT secret was not configured.");
                }

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.Secret)),

                        RoleClaimType = ClaimTypes.Role
                    };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        services.AddHttpContextAccessor();

    services.AddScoped<ICurrentUserService,CurrentUserService>();

        services.Configure<AzureStorageSettings>(configuration.GetSection(AzureStorageSettings.SectionName));
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailNotificationSender, SmtpEmailSender>();

        services.Configure<GitHubSettings>(configuration.GetSection(GitHubSettings.SectionName));
        services.AddHttpClient<IGitHubIntegrationService, GitHubIntegrationService>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DevFlow");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }).AddStandardResilienceHandler();

        return services;
    }
}