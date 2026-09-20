using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Infrastructure.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISessionService _sessionService;

    public UserManagementService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthorizationService authorizationService,
        ISessionService sessionService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsurePermission(AppPermission.UserView);
        return await _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsurePermission(AppPermission.UserView);
        return await _userRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<User> CreateUserAsync(
        string username,
        string password,
        string displayName,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsurePermission(AppPermission.UserCreate);

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new DomainValidationException("Password must be at least 6 characters long.");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existingUser is not null)
        {
            throw new DomainValidationException($"A user with username '{username}' already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var newUser = new User(Guid.NewGuid(), username, passwordHash, displayName, role);

        await _userRepository.AddAsync(newUser, cancellationToken);
        return newUser;
    }

    public async Task<User> UpdateUserAsync(
        Guid id,
        string displayName,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsurePermission(AppPermission.UserEdit);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new DomainValidationException($"User with ID '{id}' was not found.");
        }

        // If changing role of current admin, ensure we don't remove the last active admin
        if (user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            var activeAdminCount = allUsers.Count(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);
            if (activeAdminCount == 0)
            {
                throw new DomainValidationException("Cannot change role of the last active Administrator.");
            }
        }

        user.UpdateDetails(displayName, role);
        await _userRepository.UpdateAsync(user, cancellationToken);
        return user;
    }

    public async Task SetUserActiveStateAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        _authorizationService.EnsurePermission(AppPermission.UserDeactivate);

        var currentUserId = _sessionService.CurrentUser?.Id;
        if (currentUserId == id && !isActive)
        {
            throw new DomainValidationException("Cannot deactivate the currently logged in user.");
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new DomainValidationException($"User with ID '{id}' was not found.");
        }

        if (!isActive && user.Role == UserRole.Admin)
        {
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            var activeAdminCount = allUsers.Count(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);
            if (activeAdminCount == 0)
            {
                throw new DomainValidationException("Cannot deactivate the last active Administrator.");
            }
        }

        if (isActive)
            user.Activate();
        else
            user.Deactivate();

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
