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

        var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()  ?? throw new InvalidOperationException("JWT settings were not configured.");

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
    });

    services.AddAuthorization();

        return services;
    }
}