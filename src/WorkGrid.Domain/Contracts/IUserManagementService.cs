using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;

namespace WorkGrid.Domain.Contracts;

public interface IUserManagementService
{
    Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(string username, string password, string displayName, UserRole role, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(Guid id, string displayName, UserRole role, CancellationToken cancellationToken = default);
    Task SetUserActiveStateAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
