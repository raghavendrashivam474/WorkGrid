using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class AuthorizationServiceTests
{
    private readonly SessionService _sessionService = new();
    private readonly AuthorizationService _authzService;

    public AuthorizationServiceTests()
    {
        _authzService = new AuthorizationService(_sessionService);
    }

    [Fact]
    public void Viewer_HasViewPermissions_ButNoMutationsOrUserManagement()
    {
        var viewer = new User(Guid.NewGuid(), "viewer", "hash", "Viewer", UserRole.Viewer);
        _sessionService.StartSession(viewer);

        // Allowed
        Assert.True(_authzService.HasPermission(AppPermission.EmployeeView));
        Assert.True(_authzService.HasPermission(AppPermission.AssetView));
        Assert.True(_authzService.HasPermission(AppPermission.AssignmentView));

        // Disallowed
        Assert.False(_authzService.HasPermission(AppPermission.EmployeeCreate));
        Assert.False(_authzService.HasPermission(AppPermission.AssetCreate));
        Assert.False(_authzService.HasPermission(AppPermission.AssignmentCreate));
        Assert.False(_authzService.HasPermission(AppPermission.UserView));
        Assert.False(_authzService.HasPermission(AppPermission.UserCreate));
    }

    [Fact]
    public void Manager_HasOperationalPermissions_ButNoUserManagementOrDeletions()
    {
        var manager = new User(Guid.NewGuid(), "manager", "hash", "Manager", UserRole.Manager);
        _sessionService.StartSession(manager);

        // Allowed
        Assert.True(_authzService.HasPermission(AppPermission.EmployeeCreate));
        Assert.True(_authzService.HasPermission(AppPermission.AssetCreate));
        Assert.True(_authzService.HasPermission(AppPermission.AssetMaintenance));
        Assert.True(_authzService.HasPermission(AppPermission.AssetRetire));
        Assert.True(_authzService.HasPermission(AppPermission.AssignmentCreate));
        Assert.True(_authzService.HasPermission(AppPermission.AssignmentReturn));

        // Disallowed
        Assert.False(_authzService.HasPermission(AppPermission.EmployeeDelete));
        Assert.False(_authzService.HasPermission(AppPermission.AssetDelete));
        Assert.False(_authzService.HasPermission(AppPermission.UserView));
        Assert.False(_authzService.HasPermission(AppPermission.UserCreate));
    }

    [Fact]
    public void Admin_HasAllPermissions()
    {
        var admin = new User(Guid.NewGuid(), "admin", "hash", "Admin", UserRole.Admin);
        _sessionService.StartSession(admin);

        foreach (AppPermission perm in Enum.GetValues<AppPermission>())
        {
            Assert.True(_authzService.HasPermission(perm), $"Admin should have permission {perm}");
        }
    }

    [Fact]
    public void InactiveUser_HasNoPermissions()
    {
        var inactiveAdmin = new User(Guid.NewGuid(), "admin", "hash", "Admin", UserRole.Admin, isActive: false);

        Assert.False(_authzService.HasPermission(inactiveAdmin, AppPermission.EmployeeView));
        Assert.False(_authzService.HasPermission(inactiveAdmin, AppPermission.UserView));
    }

    [Fact]
    public void EnsurePermission_WhenLackingPermission_ThrowsAuthorizationException()
    {
        var viewer = new User(Guid.NewGuid(), "viewer", "hash", "Viewer", UserRole.Viewer);
        _sessionService.StartSession(viewer);

        var ex = Assert.Throws<AuthorizationException>(() =>
            _authzService.EnsurePermission(AppPermission.AssetCreate));

        Assert.Contains("lacks permission 'AssetCreate'", ex.Message);
    }
}
