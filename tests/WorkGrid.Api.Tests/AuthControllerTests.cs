using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkGrid.Api.Controllers;
using WorkGrid.Api.Models;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class AuthControllerTests
{
    private sealed class MockLogger : ILogger<AuthController>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class MockUserRepository : IUserRepository
    {
        public User? StoredUser { get; set; }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            if (StoredUser != null && string.Equals(StoredUser.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<User?>(StoredUser);
            }
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<System.Collections.Generic.IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var tokenService = new JwtTokenService();
        var user = new User(Guid.NewGuid(), "admin", hasher.HashPassword("AdminPass123!"), "Administrator", UserRole.Admin);

        var repo = new MockUserRepository { StoredUser = user };
        var controller = new AuthController(repo, hasher, tokenService, new MockLogger());

        // Act
        var result = await controller.Login(new LoginRequest("admin", "AdminPass123!"), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("admin", response.Username);
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var tokenService = new JwtTokenService();
        var user = new User(Guid.NewGuid(), "admin", hasher.HashPassword("AdminPass123!"), "Administrator", UserRole.Admin);

        var repo = new MockUserRepository { StoredUser = user };
        var controller = new AuthController(repo, hasher, tokenService, new MockLogger());

        // Act
        var result = await controller.Login(new LoginRequest("admin", "WrongPassword!"), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var tokenService = new JwtTokenService();
        var user = new User(Guid.NewGuid(), "inactive_user", hasher.HashPassword("ValidPass123!"), "Inactive", UserRole.Viewer);
        user.Deactivate();

        var repo = new MockUserRepository { StoredUser = user };
        var controller = new AuthController(repo, hasher, tokenService, new MockLogger());

        // Act
        var result = await controller.Login(new LoginRequest("inactive_user", "ValidPass123!"), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
