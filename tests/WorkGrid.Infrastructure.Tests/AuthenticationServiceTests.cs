using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class AuthenticationServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WorkGridDbContext _context;
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _hasher;
    private readonly SessionService _sessionService;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"workgrid_auth_test_{Guid.NewGuid():N}.db3");
        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.Migrate();

        _userRepository = new UserRepository(_context);
        _hasher = new PasswordHasher();
        _sessionService = new SessionService();
        _authService = new AuthenticationService(_userRepository, _hasher, _sessionService);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task RegisterInitialAdminAsync_OnFreshDatabase_SucceedsAndStartsSession()
    {
        var result = await _authService.RegisterInitialAdminAsync("admin", "AdminPass123!", "System Administrator");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("admin", result.User.Username);
        Assert.Equal(UserRole.Admin, result.User.Role);
        Assert.True(_sessionService.IsAuthenticated);
        Assert.Equal(result.User.Id, _sessionService.CurrentUser?.Id);
    }

    [Fact]
    public async Task RegisterInitialAdminAsync_WhenUsersExist_Fails()
    {
        await _authService.RegisterInitialAdminAsync("admin", "AdminPass123!", "System Administrator");

        var secondResult = await _authService.RegisterInitialAdminAsync("admin2", "AdminPass123!", "Second Admin");

        Assert.False(secondResult.Success);
        Assert.Contains("already initialized", secondResult.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_SucceedsAndRecordsLogin()
    {
        var passHash = _hasher.HashPassword("SecretPassword123!");
        var user = new User(Guid.NewGuid(), "john_doe", passHash, "John Doe", UserRole.Manager);
        await _userRepository.AddAsync(user);

        var result = await _authService.LoginAsync("john_doe", "SecretPassword123!");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.NotNull(result.User.LastLoginAt);
        Assert.True(_sessionService.IsAuthenticated);
        Assert.Equal("john_doe", _sessionService.CurrentUser?.Username);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_FailsAndDoesNotStartSession()
    {
        var passHash = _hasher.HashPassword("SecretPassword123!");
        var user = new User(Guid.NewGuid(), "john_doe", passHash, "John Doe", UserRole.Manager);
        await _userRepository.AddAsync(user);

        var result = await _authService.LoginAsync("john_doe", "WrongPassword!");

        Assert.False(result.Success);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        Assert.False(_sessionService.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_Fails()
    {
        var passHash = _hasher.HashPassword("SecretPassword123!");
        var user = new User(Guid.NewGuid(), "inactive_user", passHash, "Inactive User", UserRole.Viewer, isActive: false);
        await _userRepository.AddAsync(user);

        var result = await _authService.LoginAsync("inactive_user", "SecretPassword123!");

        Assert.False(result.Success);
        Assert.Contains("inactive", result.ErrorMessage);
        Assert.False(_sessionService.IsAuthenticated);
    }

    [Fact]
    public async Task Logout_ClearsActiveSession()
    {
        var passHash = _hasher.HashPassword("SecretPassword123!");
        var user = new User(Guid.NewGuid(), "active_user", passHash, "Active User", UserRole.Viewer);
        await _userRepository.AddAsync(user);

        await _authService.LoginAsync("active_user", "SecretPassword123!");
        Assert.True(_sessionService.IsAuthenticated);

        _authService.Logout();

        Assert.False(_sessionService.IsAuthenticated);
        Assert.Null(_sessionService.CurrentUser);
    }
}
