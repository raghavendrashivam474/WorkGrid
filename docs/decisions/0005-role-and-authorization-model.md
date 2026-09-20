# ADR-0005: Role and Authorization Model

## Context
WorkGrid operations (creating/editing/deleting employees and assets, assigning and returning physical equipment, user provisioning) require access control based on user roles (`Admin`, `Manager`, `Viewer`). UI controls alone do not constitute security; permissions must be enforced at both the business-operation boundary and reflected in the user interface.

## Decision
1. **Granular Permissions**: Defined via `AppPermission` enum categorizing capabilities (`Employee*`, `Asset*`, `Assignment*`, `User*`).
2. **Centralized Authorization**: Implemented `IAuthorizationService` mapping roles to frozen permission sets and evaluating against the active `ISessionService` user.
3. **Defense in Depth**:
   - Business services (`AssignmentService`, `UserManagementService`) execute `_authorizationService.EnsurePermission(...)` and throw `AuthorizationException` if unauthorized.
   - ViewModels query `HasPermission(...)` to hide or disable UI command triggers.
4. **Administrative Safeguards**:
   - An administrator cannot deactivate their own active account.
   - The system strictly blocks removing or deactivating the last active Administrator.

## Permission Matrix
| Permission | Admin | Manager | Viewer |
|---|:---:|:---:|:---:|
| Employee View | ✅ | ✅ | ✅ |
| Employee Create / Edit | ✅ | ✅ | ❌ |
| Employee Delete | ✅ | ❌ | ❌ |
| Asset View | ✅ | ✅ | ✅ |
| Asset Create / Edit / Maintenance / Retire | ✅ | ✅ | ❌ |
| Asset Delete | ✅ | ❌ | ❌ |
| Assignment View | ✅ | ✅ | ✅ |
| Assignment Create / Return | ✅ | ✅ | ❌ |
| User View / Create / Edit / Deactivate | ✅ | ❌ | ❌ |
