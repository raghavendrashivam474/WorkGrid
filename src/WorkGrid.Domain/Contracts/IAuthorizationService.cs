using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;

namespace WorkGrid.Domain.Contracts;

public interface IAuthorizationService
{
    bool HasPermission(AppPermission permission);
    bool HasPermission(User? user, AppPermission permission);
    void EnsurePermission(AppPermission permission);
    void EnsurePermission(User? user, AppPermission permission);
    IReadOnlySet<AppPermission> GetPermissionsForRole(UserRole role);
}
