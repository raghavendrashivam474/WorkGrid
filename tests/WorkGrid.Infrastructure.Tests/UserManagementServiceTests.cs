using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class UserManagementServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WorkGridDbContext _context;
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authzService;
    private readonly UserManagementService _mgmtService;
    private readonly User _adminUser;

    public UserManagementServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"workgrid_usermgmt_{Guid.NewGuid():N}.db3");
        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.Migrate();

        _userRepository = new UserRepository(_context);
        _passwordHasher = new PasswordHasher();
        _sessionService = new SessionService();
        _authzService = new AuthorizationService(_sessionService);
        _mgmtService = new UserManagementService(_userRepository, _passwordHasher, _authzService, _sessionService);

        _adminUser = new User(Guid.NewGuid(), "admin", _passwordHasher.HashPassword("Pass123!"), "Admin User", UserRole.Admin);
        _userRepository.AddAsync(_adminUser).GetAwaiter().GetResult();
        _sessionService.StartSession(_adminUser);
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
    public async Task CreateUserAsync_AsAdmin_CreatesUserSuccessfully()
    {
        var newUser = await _mgmtService.CreateUserAsync("newmgr", "ManagerPass123!", "New Manager", UserRole.Manager);

        Assert.NotNull(newUser);
        Assert.Equal("newmgr", newUser.Username);
        Assert.Equal(UserRole.Manager, newUser.Role);
        Assert.True(newUser.IsActive);
    }

    [Fact]
    public async Task CreateUserAsync_AsManager_ThrowsAuthorizationException()
    {
        var manager = new User(Guid.NewGuid(), "mgr", "hash", "Manager", UserRole.Manager);
        _sessionService.StartSession(manager);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _mgmtService.CreateUserAsync("newuser", "Pass1234!", "New User", UserRole.Viewer));
    }

    [Fact]
    public async Task SetUserActiveStateAsync_CannotDeactivateSelf()
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _mgmtService.SetUserActiveStateAsync(_adminUser.Id, false));

        Assert.Contains("Cannot deactivate the currently logged in user", ex.Message);
    }

    [Fact]
    public async Task SetUserActiveStateAsync_CannotDeactivateLastActiveAdmin()
    {
        // Add a second admin to execute the call
        var admin2 = await _mgmtService.CreateUserAsync("admin2", "Pass1234!", "Admin 2", UserRole.Admin);
        _sessionService.StartSession(admin2);

        // Deactivate first admin -> OK (1 admin remains)
        await _mgmtService.SetUserActiveStateAsync(_adminUser.Id, false);

        // Attempting to deactivate admin2 (last active admin) should fail
        // Switch session or call:
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _mgmtService.SetUserActiveStateAsync(admin2.Id, false));

        Assert.Contains("Cannot deactivate", ex.Message);
    }
}
