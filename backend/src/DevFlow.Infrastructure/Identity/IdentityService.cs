using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<bool> UserExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await userManager.FindByEmailAsync(email) is not null;

    public async Task<UserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await ToUserInfoAsync(await userManager.FindByEmailAsync(email));

    public async Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await ToUserInfoAsync(await userManager.FindByIdAsync(userId.ToString()));

    public async Task<IReadOnlyDictionary<Guid, UserSummary>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        return await userManager.Users
            .Where(user => ids.Contains(user.Id))
            .Select(user => new UserSummary(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
    }

    public async Task<UserCreationResult> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            UserName = email,
            Email = email,
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? new UserCreationResult(true, user.Id, [])
            : new UserCreationResult(false, null, result.Errors.Select(error => error.Description));
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return true;
        }

        // Counts the failure and locks the account after Identity's MaxFailedAccessAttempts.
        await userManager.AccessFailedAsync(user);
        return false;
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? [] : (await userManager.GetRolesAsync(user)).ToArray();
    }

    public async Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User '{userId}' was not found.");

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to assign role '{role}': {errors}");
        }
    }

    private async Task<UserInfo?> ToUserInfoAsync(ApplicationUser? user)
    {
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserInfo(user.Id, user.FirstName, user.LastName, user.Email!, roles.ToArray());
    }
}
