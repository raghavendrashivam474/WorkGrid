using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Infrastructure.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionService _sessionService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionService sessionService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult(false, "Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return new AuthenticationResult(false, "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return new AuthenticationResult(false, "This user account is inactive. Please contact an administrator.");
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new AuthenticationResult(false, "Invalid username or password.");
        }

        user.RecordLogin(DateTimeOffset.UtcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _sessionService.StartSession(user);

        return new AuthenticationResult(true, User: user);
    }

    public void Logout()
    {
        _sessionService.EndSession();
    }

    public async Task<AuthenticationResult> RegisterInitialAdminAsync(string username, string password, string displayName, CancellationToken cancellationToken = default)
    {
        var hasUsers = await _userRepository.HasAnyUsersAsync(cancellationToken);
        if (hasUsers)
        {
            return new AuthenticationResult(false, "System already initialized. Initial admin can only be created on a fresh database.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return new AuthenticationResult(false, "Password must be at least 6 characters long.");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var admin = new User(Guid.NewGuid(), username, passwordHash, displayName, UserRole.Admin);

        await _userRepository.AddAsync(admin, cancellationToken);
        _sessionService.StartSession(admin);

        return new AuthenticationResult(true, User: admin);
    }

    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.HasAnyUsersAsync(cancellationToken);
    }
}
