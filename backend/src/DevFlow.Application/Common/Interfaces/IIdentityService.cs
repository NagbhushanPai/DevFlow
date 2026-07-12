using DevFlow.Application.Common.Models;

namespace DevFlow.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> UserExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<UserInfo?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<UserCreationResult> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);
}