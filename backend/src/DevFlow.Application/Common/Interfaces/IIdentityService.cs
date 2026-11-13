using DevFlow.Application.Common.Models;

namespace DevFlow.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> UserExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns basic profile data for the given users, keyed by id (unknown ids are omitted).</summary>
    Task<IReadOnlyDictionary<Guid, UserSummary>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    Task<UserCreationResult> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the password, honouring account lockout: failed attempts are counted and a locked-out
    /// account is rejected even with the right password.
    /// </summary>
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
