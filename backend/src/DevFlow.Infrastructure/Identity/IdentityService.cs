using DevFlow.Application.Common.Interfaces;
using DevFlow.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace DevFlow.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        return user is not null;
    }

    public async Task<UserInfo?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        return new UserInfo(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!);
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
            Email = email
        };

        var result = await _userManager.CreateAsync(
            user,
            password);

        if (!result.Succeeded)
        {
            return new UserCreationResult(
                false,
                null,
                result.Errors.Select(error => error.Description));
        }

        return new UserCreationResult(
            true,
            user.Id,
            []);
    }

    public async Task<bool> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(
            user,
            password);
    }
}