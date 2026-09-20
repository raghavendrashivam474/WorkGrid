using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class SecurityInvariantTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WorkGridDbContext _context;
    private readonly UserRepository _userRepository;
    private readonly EmployeeRepository _employeeRepository;
    private readonly AssetRepository _assetRepository;
    private readonly AssignmentRepository _assignmentRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;
    private readonly AuthenticationService _authService;
    private readonly UserManagementService _userManagementService;
    private readonly AssignmentService _assignmentService;

    public SecurityInvariantTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"workgrid_security_{Guid.NewGuid():N}.db3");
        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.Migrate();

        _userRepository = new UserRepository(_context);
        _employeeRepository = new EmployeeRepository(_context);
        _assetRepository = new AssetRepository(_context);
        _assignmentRepository = new AssignmentRepository(_context);

        _passwordHasher = new PasswordHasher();
        _sessionService = new SessionService();
        _authorizationService = new AuthorizationService(_sessionService);

        _authService = new AuthenticationService(_userRepository, _passwordHasher, _sessionService);
        _userManagementService = new UserManagementService(_userRepository, _passwordHasher, _authorizationService, _sessionService);
        _assignmentService = new AssignmentService(_assignmentRepository, _assetRepository, _employeeRepository, _context, _authorizationService);
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
    public async Task PasswordHashing_NeverStoresPlaintext_AndHashesAreIrreversible()
    {
        const string rawPassword = "P@ssw0rdSecure2026!";
        var user = await _userManagementServiceCreateDirectAsync("secuser", rawPassword, "Sec User", UserRole.Viewer);

        var retrieved = await _userRepository.GetByIdAsync(user.Id);
        Assert.NotNull(retrieved);
        Assert.NotEqual(rawPassword, retrieved.PasswordHash);
        Assert.DoesNotContain(rawPassword, retrieved.PasswordHash);
    }

    [Fact]
    public async Task InactiveUser_CannotAuthenticate_AndCannotStartSession()
    {
        var user = await _userManagementServiceCreateDirectAsync("inact1", "Password123!", "Inactive 1", UserRole.Viewer, isActive: false);

        var result = await _authService.LoginAsync("inact1", "Password123!");

        Assert.False(result.Success);
        Assert.Contains("inactive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(_sessionService.IsAuthenticated);
        Assert.Null(_sessionService.CurrentUser);
    }

    [Fact]
    public async Task Viewer_CannotPerformAssignment_OrReturnOperations()
    {
        var viewer = await _userManagementServiceCreateDirectAsync("view1", "Password123!", "Viewer 1", UserRole.Viewer);
        _sessionService.StartSession(viewer);

        var emp = new Employee(Guid.NewGuid(), "EMP-S1", "Test Emp", "test@workgrid.local");
        var ast = new Asset(Guid.NewGuid(), "AST-S1", "Laptop", "HW", "SN1");
        await _employeeRepository.AddAsync(emp);
        await _assetRepository.AddAsync(ast);

        // Viewer cannot assign
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _assignmentService.AssignAssetAsync(emp.Id, ast.Id));

        // Viewer cannot return
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _assignmentService.ReturnAssetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Viewer_CannotAccessUserManagement()
    {
        var viewer = await _userManagementServiceCreateDirectAsync("view2", "Password123!", "Viewer 2", UserRole.Viewer);
        _sessionService.StartSession(viewer);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _userManagementService.GetAllUsersAsync());

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _userManagementService.CreateUserAsync("hackuser", "Pass123!", "Hacker", UserRole.Admin));
    }

    [Fact]
    public async Task Manager_CannotManageUsers()
    {
        var manager = await _userManagementServiceCreateDirectAsync("mgr1", "Password123!", "Manager 1", UserRole.Manager);
        _sessionService.StartSession(manager);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _userManagementService.GetAllUsersAsync());

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _userManagementService.CreateUserAsync("hackuser2", "Pass123!", "Hacker 2", UserRole.Admin));
    }

    [Fact]
    public async Task Admin_CannotDeactivateSelf()
    {
        var admin = await _userManagementServiceCreateDirectAsync("admin1", "Password123!", "Admin 1", UserRole.Admin);
        _sessionService.StartSession(admin);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _userManagementService.SetUserActiveStateAsync(admin.Id, false));

        Assert.Contains("Cannot deactivate the currently logged in user", ex.Message);
    }

    [Fact]
    public async Task Admin_CannotDemoteLastActiveAdmin()
    {
        var admin = await _userManagementServiceCreateDirectAsync("admin_only", "Password123!", "Admin Only", UserRole.Admin);
        _sessionService.StartSession(admin);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _userManagementService.UpdateUserAsync(admin.Id, "Admin Only", UserRole.Viewer));

        Assert.Contains("last active Administrator", ex.Message);
    }

    [Fact]
    public async Task Session_IsFullyIsolated_AndLogoutTerminatesAccess()
    {
        var admin = await _userManagementServiceCreateDirectAsync("admin_sess", "Password123!", "Admin Sess", UserRole.Admin);
        await _authService.LoginAsync("admin_sess", "Password123!");
        Assert.True(_sessionService.IsAuthenticated);

        _authService.Logout();

        Assert.False(_sessionService.IsAuthenticated);
        Assert.Null(_sessionService.CurrentUser);

        // Any further operations fail with AuthorizationException
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _userManagementService.GetAllUsersAsync());
    }

    private async Task<User> _userManagementServiceCreateDirectAsync(
        string username,
        string password,
        string displayName,
        UserRole role,
        bool isActive = true)
    {
        var hash = _passwordHasher.HashPassword(password);
        var user = new User(Guid.NewGuid(), username, hash, displayName, role, isActive);
        await _userRepository.AddAsync(user);
        return user;
    }
}
