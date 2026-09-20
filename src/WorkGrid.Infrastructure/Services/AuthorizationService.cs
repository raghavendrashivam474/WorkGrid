using System.Collections.Frozen;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Infrastructure.Services;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ISessionService _sessionService;

    private static readonly FrozenDictionary<UserRole, HashSet<AppPermission>> RolePermissions =
        new Dictionary<UserRole, HashSet<AppPermission>>
        {
            [UserRole.Viewer] = new()
            {
                AppPermission.EmployeeView,
                AppPermission.AssetView,
                AppPermission.AssignmentView
            },
            [UserRole.Manager] = new()
            {
                AppPermission.EmployeeView,
                AppPermission.EmployeeCreate,
                AppPermission.EmployeeEdit,
                AppPermission.AssetView,
                AppPermission.AssetCreate,
                AppPermission.AssetEdit,
                AppPermission.AssetMaintenance,
                AppPermission.AssetRetire,
                AppPermission.AssignmentView,
                AppPermission.AssignmentCreate,
                AppPermission.AssignmentReturn
            },
            [UserRole.Admin] = new()
            {
                // Full Access across all features
                AppPermission.EmployeeView,
                AppPermission.EmployeeCreate,
                AppPermission.EmployeeEdit,
                AppPermission.EmployeeDelete,
                AppPermission.AssetView,
                AppPermission.AssetCreate,
                AppPermission.AssetEdit,
                AppPermission.AssetDelete,
                AppPermission.AssetMaintenance,
                AppPermission.AssetRetire,
                AppPermission.AssignmentView,
                AppPermission.AssignmentCreate,
                AppPermission.AssignmentReturn,
                AppPermission.UserView,
                AppPermission.UserCreate,
                AppPermission.UserEdit,
                AppPermission.UserDeactivate
            }
        }.ToFrozenDictionary();

    public AuthorizationService(ISessionService sessionService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    public bool HasPermission(AppPermission permission)
    {
        return HasPermission(_sessionService.CurrentUser, permission);
    }

    public bool HasPermission(User? user, AppPermission permission)
    {
        if (user is null || !user.IsActive)
            return false;

        if (RolePermissions.TryGetValue(user.Role, out var permissions))
        {
            return permissions.Contains(permission);
        }

        return false;
    }

    public void EnsurePermission(AppPermission permission)
    {
        EnsurePermission(_sessionService.CurrentUser, permission);
    }

    public void EnsurePermission(User? user, AppPermission permission)
    {
        if (user is null)
        {
            throw new AuthorizationException("Operation denied: No authenticated user session found.");
        }

        if (!user.IsActive)
        {
            throw new AuthorizationException($"Operation denied: User account '{user.Username}' is inactive.");
        }

        if (!HasPermission(user, permission))
        {
            throw new AuthorizationException(
                $"Operation denied: User '{user.Username}' with role '{user.Role}' lacks permission '{permission}'.");
        }
    }

    public IReadOnlySet<AppPermission> GetPermissionsForRole(UserRole role)
    {
        if (RolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions;
        }

        return new HashSet<AppPermission>();
    }
}
