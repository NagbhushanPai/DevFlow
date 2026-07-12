using DevFlow.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DevFlow.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        string[] roles =
        [
            Roles.Admin,
            Roles.Manager,
            Roles.Developer
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(role));
            }
        }
    }
}