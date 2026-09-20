using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using Xunit;

namespace WorkGrid.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesUserSuccessfully()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "admin", "hash123", "System Admin", UserRole.Admin);

        Assert.Equal(id, user.Id);
        Assert.Equal("admin", user.Username);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal("System Admin", user.DisplayName);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.IsActive);
        Assert.Null(user.LastLoginAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")] // Less than 3 characters
    public void Constructor_WithInvalidUsername_ThrowsDomainValidationException(string username)
    {
        Assert.Throws<DomainValidationException>(() =>
            new User(Guid.NewGuid(), username, "hash123", "Test User", UserRole.Viewer));
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new User(Guid.Empty, "validuser", "hash123", "Test User", UserRole.Viewer));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyPasswordHash_ThrowsDomainValidationException(string? hash)
    {
        Assert.Throws<DomainValidationException>(() =>
            new User(Guid.NewGuid(), "validuser", hash!, "Test User", UserRole.Viewer));
    }

    [Fact]
    public void UpdateDetails_UpdatesDisplayNameAndRole()
    {
        var user = new User(Guid.NewGuid(), "manager1", "hash123", "Old Name", UserRole.Viewer);
        user.UpdateDetails("New Manager", UserRole.Manager);

        Assert.Equal("New Manager", user.DisplayName);
        Assert.Equal(UserRole.Manager, user.Role);
    }

    [Fact]
    public void DeactivateAndActivate_TogglesIsActiveState()
    {
        var user = new User(Guid.NewGuid(), "user1", "hash123", "User One", UserRole.Viewer);
        Assert.True(user.IsActive);

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void RecordLogin_WhenActive_SetsLastLoginAt()
    {
        var user = new User(Guid.NewGuid(), "user1", "hash123", "User One", UserRole.Viewer);
        var now = DateTimeOffset.UtcNow;

        user.RecordLogin(now);
        Assert.Equal(now, user.LastLoginAt);
    }

    [Fact]
    public void RecordLogin_WhenInactive_ThrowsDomainValidationException()
    {
        var user = new User(Guid.NewGuid(), "user1", "hash123", "User One", UserRole.Viewer, isActive: false);

        Assert.Throws<DomainValidationException>(() => user.RecordLogin(DateTimeOffset.UtcNow));
    }
}
