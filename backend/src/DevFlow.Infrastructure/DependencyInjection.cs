using System.Security.Claims;
using System.Text;
using DevFlow.Application.Common.Interfaces;
using DevFlow.Infrastructure.Auditing;
using DevFlow.Infrastructure.Authentication;
using DevFlow.Infrastructure.Caching;
using DevFlow.Infrastructure.Email;
using DevFlow.Infrastructure.GitHub;
using DevFlow.Infrastructure.Identity;
using DevFlow.Infrastructure.Persistence;
using DevFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevFlow.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Persistence, identity, caching, storage, email, GitHub and auditing. Does <b>not</b> configure an
    /// authentication scheme: the API adds JWT (<see cref="AddJwtAuthentication"/>), the Admin app adds cookies.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>(name: "database");

        AddCaching(services, configuration);
        AddIdentity(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.Configure<AzureStorageSettings>(configuration.GetSection(AzureStorageSettings.SectionName));
        services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<SmtpEmailSender>();
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailNotificationSender, QueuedEmailSender>();
        services.AddHostedService<EmailDispatcherService>();

        services.AddSingleton<AuditLogQueue>();
        services.AddHostedService<AuditLogWriterService>();

        services.Configure<GitHubSettings>(configuration.GetSection(GitHubSettings.SectionName));
        services.AddHttpClient<IGitHubIntegrationService, GitHubIntegrationService>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DevFlow");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }).AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// JWT bearer authentication for the API. JwtSettings are validated at startup (secret must be at least
    /// 32 bytes), so a misconfigured deployment fails fast instead of issuing weak tokens.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    // Browsers cannot set headers on WebSocket connections, so SignalR passes the token in the query.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "devflow:";
            });
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();
    }
}
