using DevFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;

namespace DevFlow.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"DevFlowTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(AppContext.BaseDirectory);

            config.AddJsonFile(
                "appsettings.Testing.json",
                optional: false,
                reloadOnChange: false);

            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{DevFlow.Infrastructure.Authentication.JwtSettings.SectionName}:Secret"] =
                        "DevFlowIntegrationTestsSecretKey12345678901234567890",
                    [$"{DevFlow.Infrastructure.Authentication.JwtSettings.SectionName}:Issuer"] =
                        "DevFlow",
                    [$"{DevFlow.Infrastructure.Authentication.JwtSettings.SectionName}:Audience"] =
                        "DevFlow",
                    [$"{DevFlow.Infrastructure.Authentication.JwtSettings.SectionName}:ExpirationInMinutes"] =
                        "60"
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(
                options =>
                {
                    options.UseInMemoryDatabase(
                        _databaseName);
                });
        });
    }
}
