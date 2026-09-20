using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public User(
        Guid id,
        string username,
        string passwordHash,
        string displayName,
        UserRole role,
        bool isActive = true,
        DateTimeOffset createdAt = default,
        DateTimeOffset? lastLoginAt = null)
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(username))
            throw new DomainValidationException("Username cannot be null or whitespace.");

        if (username.Trim().Length < 3)
            throw new DomainValidationException("Username must be at least 3 characters long.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainValidationException("Password hash cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainValidationException("Display name cannot be null or whitespace.");

        Id = id;
        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        DisplayName = displayName.Trim();
        Role = role;
        IsActive = isActive;
        CreatedAt = createdAt == default ? DateTimeOffset.UtcNow : createdAt;
        LastLoginAt = lastLoginAt;
    }

    public void UpdateDetails(string displayName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainValidationException("Display name cannot be null or whitespace.");

        DisplayName = displayName.Trim();
        Role = role;
    }

    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainValidationException("Password hash cannot be null or empty.");

        PasswordHash = newPasswordHash;
    }

    public void RecordLogin(DateTimeOffset loginTime)
    {
        if (!IsActive)
            throw new DomainValidationException("Cannot record login for an inactive user.");

        LastLoginAt = loginTime;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
