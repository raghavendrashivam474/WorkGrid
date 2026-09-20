using WorkGrid.Domain.Entities;

namespace WorkGrid.Domain.Contracts;

public record AuthenticationResult(bool Success, string? ErrorMessage = null, User? User = null);

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    void Logout();
    Task<AuthenticationResult> RegisterInitialAdminAsync(string username, string password, string displayName, CancellationToken cancellationToken = default);
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
}
