# WorkGrid — Android v1.0 Product Hardening
## Post-Campaign Engineering Report: AV1.5 → AV1.9

**Prepared by:** Junior Developer (Hardening Track)
**Reviewed against baseline:** v-AV1.1-AV1.4 (120/120 tests, 0 warnings, 0 errors)
**Final state:** v-AV1.5-AV1.9 (121/121 tests, 0 warnings, 0 errors)
**Date:** 2025-07-12

---

## 1. Executive Summary

The AV1.5–AV1.9 hardening campaign was executed across five sequential sprints with a single governing constraint: **harden what exists without rebuilding what exists.** No architectural rewrites were performed. No new frameworks were introduced. No Domain, Infrastructure, API, or Remote boundaries were modified. All changes were confined to the App presentation layer (ViewModels and XAML Views) and one new Infrastructure test file.

The campaign hardened the four remaining unhardened surfaces identified in the AV1.1–AV1.4 report (Assignments, Users, global state consistency, and navigation), eliminated legacy dead code, and produced automated behavioral proof of the local-first persistence promise.

---

## 2. Sprint-by-Sprint Implementation Detail

### 2.1 AV1.5 — Assignment & Workflow Hardening

**Scope:** `AssignmentListViewModel`, `AssignmentDetailViewModel`, `AssignmentListPage.xaml`, `AssignmentDetailPage.xaml`

**What was implemented:**

| Gap ID | Problem | Implementation |
|--------|---------|----------------|
| G1 | `SelectAssignmentCommand` had no navigation guard. Rapid taps pushed duplicate `AssignmentDetailPage` instances onto the Shell navigation stack. | Added `_isNavigating` boolean field. Wrapped `Shell.Current.GoToAsync()` in try/finally blocks that set and reset the flag. Bound `CanExecute` predicate to `!IsLoading && !_isNavigating`. |
| G2 | `AddAssignmentCommand` had the same duplicate-push vulnerability. | Same `_isNavigating` guard pattern applied. |
| G3 | No visible loading indicator during initial assignment list fetch. The screen appeared blank between `OnAppearing()` and data arrival. | Added a centered `ActivityIndicator` overlay with `ZIndex="10"` bound to `IsLoading`. |
| G4 | `ReturnAssetAsync` executed immediately on button tap with no user confirmation. Accidental taps caused irreversible state changes. | Added `Application.Current.MainPage.DisplayAlert("Confirm Return", ...)` before invoking the service method. |
| G5 | `HorizontalStackLayout` containing `AssetTag` + `AssetName` labels caused layout overflow on narrow Android viewports. | Replaced the `HorizontalStackLayout` with a single `Label` using `FormattedText` with nested `Span` elements and `LineBreakMode="TailTruncation"`. |
| G6 | Commands did not call `ChangeCanExecute()` when `IsLoading` toggled, leaving buttons clickable during active operations. | Modified the `IsLoading` property setter to call `((Command)XxxCommand).ChangeCanExecute()` on all three commands. |
| G7 | The "+ Assign Asset" button remained enabled during background loads. | Bound `IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBoolConverter}}"`. |
| G9 | `ReturnedAt` date was bound to `IsNotNullOrEmptyConverter`, which only handles `string` types. A `DateTimeOffset?` value always evaluated as `false`, so returned dates never rendered. | Replaced the binding with `IsVisible="{Binding IsActive, Converter={StaticResource InverseBoolConverter}}"`, which correctly shows the returned date when the assignment is no longer active. |

**How it was implemented:**

The existing AV1.4 patterns in `AssetListViewModel` were used as the direct template. The `_isNavigating` guard, `CanExecute` delegate wiring, and `ChangeCanExecute()` calls were replicated exactly — no new abstractions were created. The `DisplayAlert` confirmation pattern was copied from `AssetDetailViewModel` and `EmployeeDetailViewModel`.

**Problems encountered and mitigations:**

- **Problem:** The `IsNotNullOrEmptyConverter` issue (G9) was subtle. The converter's `Convert` method casts the incoming value to `string` via `value as string`, which returns `null` for any non-string type including `DateTimeOffset?`. This meant the `ReturnedAt` label was permanently invisible even when populated.
- **Mitigation:** Rather than modifying the converter (which would risk breaking other bindings), we changed the XAML binding to use the boolean `IsActive` property with `InverseBoolConverter`, which is semantically correct and type-safe.

---

### 2.2 AV1.6 — Permission & User Experience Audit

**Scope:** `UserListViewModel`, `UserListPage.xaml`

**What was implemented:**

| Gap ID | Problem | Implementation |
|--------|---------|----------------|
| G1 | `UserListViewModel` implemented `INotifyPropertyChanged` directly with its own `SetField<T>` helper, duplicating logic already present in `ViewModelBase`. | Refactored to inherit from `ViewModelBase`, replacing all `SetField` calls with `SetProperty`. |
| G2 | Commands lacked `ChangeCanExecute()` notifications. Users could double-click "Create User" or "Toggle Status" during active database writes, causing concurrent mutation attempts. | Modified the `IsBusy` setter to call `ChangeCanExecute()` on all three commands. |
| G3 | The user status label used `Converter={x:Null}`, which is an invalid XAML binding expression. The label rendered raw `True`/`False` strings or failed silently. | Replaced with `DataTrigger` elements bound to `IsActive` that set `Text` to `"Active"` or `"Inactive"`, paired with `ActiveStatusColorConverter` for color coding. |
| G4 | No loading indicator during user list fetch or toggle operations. | Added `ActivityIndicator` overlay and wrapped the `CollectionView` in a `RefreshView`. |
| G5 | Long display names and usernames could push the "Toggle Status" button off-screen. | Applied `LineBreakMode="TailTruncation"` to all user info labels. |
| G6 | No client-side validation before calling `CreateUserAsync`. Empty usernames or short passwords were sent to the service layer, relying entirely on server-side rejection. | Added preemptive checks for empty username, empty display name, and password length < 6 in the ViewModel before invoking the service. |
| — | User deactivation had no confirmation guard. | Added `DisplayAlert("Confirm Deactivation", ...)` before calling `SetUserActiveStateAsync`. |

**How it was implemented:**

The permission matrix was audited against `AuthorizationService.RolePermissions` to verify that:
- **Viewer** has no user administration access (confirmed: no `User*` permissions).
- **Manager** has no user administration access (confirmed: no `User*` permissions).
- **Admin** has full `UserView`, `UserCreate`, `UserEdit`, `UserDeactivate` (confirmed).

The UI visibility of the "Add New User" form and "Toggle Status" buttons was already correctly bound to `CanManageUsers`, which delegates to `_authorizationService.HasPermission(AppPermission.UserCreate)`. No changes to the authorization architecture were needed.

**Problems encountered and mitigations:**

- **Problem:** The `Converter={x:Null}` binding was a XAML syntax error that compiled without warning but produced no meaningful output at runtime. MAUI's binding engine silently ignored the null converter reference.
- **Mitigation:** Used `DataTrigger` elements instead of a converter. This is the idiomatic MAUI approach for mapping boolean values to display strings and avoids the need for a new `BoolToActiveStatusStringConverter`.

---

### 2.3 AV1.7 — Loading, Empty & Error State Hardening

**Scope:** `HomeViewModel`, `HomePage.xaml`, `UserListViewModel`, `UserListPage.xaml`

**What was implemented:**

| Gap ID | Problem | Implementation |
|--------|---------|----------------|
| G1 | `HomeViewModel.LoadDashboardAsync()` contained an empty `catch {}` block that silently swallowed all exceptions. If the local SQLite database was locked or corrupted, the dashboard showed stale zero-count metrics with no error indication. | Replaced with `catch (Exception ex)` that sets `ErrorMessage = $"Failed to update metrics: {ex.Message}"`. Added `ErrorMessage` and `HasError` properties. |
| G1 | `LogoutCommand` had no navigation guard. Rapid taps during Shell transition could cause double-logout or navigation stack corruption. | Added `_isNavigating` guard with `ChangeCanExecute()` on the `LogoutCommand`. |
| G1 | `HomePage.xaml` had no error banner or loading indicator. | Added standardized error `Frame` (`#FFEBEE`/`#D32F2F`) and centered `ActivityIndicator` overlay. |
| G2 | `UserListViewModel` had no `IsEmpty` or `StatusMessage` properties. When no users existed, the page displayed a completely blank canvas. | Added `IsEmpty` and `StatusMessage` properties. Set `IsEmpty = Users.Count == 0` after load. |
| G2 | `UserListPage.xaml` had no empty-state template. | Added a `VerticalStackLayout` with "No Users Registered" heading and `StatusMessage` text, bound to `IsEmpty`. |
| G3 | Error banner colors were inconsistent across pages (some used `#FEE2E2`/`#EF4444`, others `#FFEBEE`/`#D32F2F`). | Standardized all error frames to `#FFEBEE` background and `#D32F2F` border/text. |

**How it was implemented:**

A global state audit was performed across all 10 ViewModels and 10 XAML Views in the application. The audit mapped every `IsLoading`, `IsBusy`, `IsEmpty`, `ErrorMessage`, `StatusMessage`, and `HasError` property to identify inconsistencies. The `HomeViewModel` and `UserListViewModel` were the only two ViewModels with material gaps.

**Problems encountered and mitigations:**

- **Problem:** The silent `catch {}` in `HomeViewModel` was a Phase 3 architectural decision intended to prevent the dashboard from crashing during initial load. However, it made debugging impossible and left users with no indication of data staleness.
- **Mitigation:** Replaced with explicit error capture that displays a non-blocking error banner. The dashboard still renders with whatever data was successfully loaded — the error message is informational, not fatal.

---

### 2.4 AV1.8 — Android UX & Navigation Polish

**Scope:** `AppShell.xaml`, `AppShell.xaml.cs`, `App.xaml.cs`, `MauiProgram.cs`, legacy file removal

**What was implemented:**

| Item | Problem | Implementation |
|------|---------|----------------|
| Legacy cleanup | `MainPage.xaml`, `MainPage.xaml.cs`, and `MainViewModel.cs` were Phase 0 scaffolding files. They were registered in `MauiProgram.cs` DI but never referenced by `AppShell`, any navigation command, or any other component. | Deleted all three files. Removed `builder.Services.AddTransient<MainViewModel>()` and `builder.Services.AddTransient<MainPage>()` from `MauiProgram.cs`. |
| Navigation audit | Verified the complete Shell route hierarchy for correctness and completeness. | Confirmed: `//login` → `LoginPage`, `//main` → `TabBar` with 5 tabs (home, employees, assets, assignments, users), 3 detail push routes (`employee-detail`, `asset-detail`, `assignment-detail`). All routes correctly registered in `AppShell.xaml.cs`. |
| Back-navigation | Verified Android hardware back button behavior across all detail pages. | All detail pages use `Shell.Current.GoToAsync("..")` protected by `_isNavigating` guards (hardened in AV1.5–AV1.6). No duplicate pops observed. |
| Startup routing | `App.xaml.cs` sets `MainPage = appShell` in the constructor, then redirects to `//login` in `OnStart()` if unauthenticated. | Verified this is the standard MAUI Shell pattern. The `login` route is declared first in `AppShell.xaml` with `FlyoutItemIsVisible="False"`, preventing it from appearing in the tab bar. |

**How it was implemented:**

A text search across the entire `src/` directory for `MainPage` and `MainViewModel` references confirmed that the only usages were:
1. The files themselves.
2. DI registrations in `MauiProgram.cs`.
3. `Application.Current?.MainPage` references in ViewModels (which refer to the MAUI `Application.MainPage` property, not the `MainPage` class).

This confirmed safe deletion.

**Problems encountered and mitigations:**

- **Problem:** Initial concern that removing `MainPage` might break the MAUI application entry point, since `App.xaml.cs` historically assigned `MainPage = new MainPage()` in template projects.
- **Mitigation:** Verified that `App.xaml.cs` already assigns `MainPage = appShell` (an `AppShell` instance), not a `MainPage` instance. The `MainPage` class was entirely orphaned. Build and test verification confirmed zero regressions.

---

### 2.5 AV1.9 — Offline & Persistence Validation

**Scope:** New test file `OfflinePersistenceValidationTests.cs`

**What was implemented:**

A comprehensive physical-disk persistence test suite that validates the entire local-first promise:

| Test Phase | What it proves |
|------------|---------------|
| Phase 1: Fresh Installation | `Database.Migrate()` creates all schemas on a clean `.db3` file. Admin registration works offline. |
| Phase 2: First Restart | After `DbContext` disposal and reinstantiation, admin login succeeds. Employee, asset, and assignment creation persist to disk. |
| Phase 3: Second Restart | All entities survive a cold start. Assignment status is `Active`. Asset status is `Assigned`. |
| Phase 4: Third Restart | Asset return completes. Asset transitions to `Maintenance`. All mutations persist. |
| Phase 5: Fourth Restart | Final verification of all aggregate states. Dashboard metrics queries return correct counts. |

**How it was implemented:**

Unlike the existing `LocalLifecyclePersistenceTests` (which use in-memory SQLite via `DataSource=:memory:`), the new test suite uses a physical `.db3` file in the system temp directory. Each "restart" is simulated by disposing the current `DbContext` and creating a new one against the same file path, exactly mirroring the Android app lifecycle where the SQLite file persists but the EF Core context is recreated on each app launch.

**Problems encountered and mitigations:**

- **Problem:** The test count increased from 120 to 121, which required verifying that the new test did not introduce flakiness due to file I/O timing.
- **Mitigation:** The test uses a unique GUID-based filename for each run (`workgrid_offline_test_{Guid}.db3`) and implements `IDisposable` to clean up the file after execution. The `try/catch` in `Dispose()` handles file lock edge cases on Windows.

---

## 3. Architectural Integrity Verification

The following components were **explicitly NOT modified** during AV1.5–AV1.9:

| Layer | Components | Status |
|-------|-----------|--------|
| Domain | `Assignment`, `Employee`, `Asset`, `User`, all enums, all contracts | ✅ Untouched |
| Infrastructure | `AssignmentService`, `UserManagementService`, `AuthorizationService`, all repositories, `WorkGridDbContext`, all configurations, all migrations | ✅ Untouched |
| API | All controllers, all remote services | ✅ Untouched |
| Authentication | `AuthenticationService`, `SessionService`, `PasswordHasher` | ✅ Untouched |
| Authorization | `AuthorizationService`, `AppPermission`, `UserRole` | ✅ Untouched |
| Persistence | SQLite configuration, EF Core setup, migration history | ✅ Untouched |

**Zero architectural change requests were filed.** No ADRs were needed. The existing architecture was sufficient for all hardening requirements.

---

## 4. Final Metrics

| Metric | Pre-Campaign (v-AV1.1-AV1.4) | Post-Campaign (v-AV1.5-AV1.9) |
|--------|------------------------------|-------------------------------|
| Total tests | 120 | 121 |
| Build warnings | 0 | 0 |
| Build errors | 0 | 0 |
| Files modified | — | 8 |
| Files deleted | — | 3 |
| Files created | — | 6 (5 docs + 1 test) |
| Sprints completed | 4 (AV1.1–AV1.4) | 5 (AV1.5–AV1.9) |
| Tags created | vSAV1.1–vSAV1.4, v-AV1.1-AV1.4 | vSAV1.5–vSAV1.9, v-AV1.5-AV1.9 |

---

## 5. Recommendations for AV1.10+

1. **Release Engineering:** The application is now functionally hardened across all major workflows. AV1.10 should focus on release packaging, Android APK signing, and store-ready metadata.
2. **Synchronization:** The local-first architecture is proven but operates in isolation from the API backend. A future phase should design the sync protocol, conflict resolution strategy, and offline queue — but this must be a deliberate architectural effort, not an incremental hardening task.
3. **Integration/E2E Tests:** The AV1.1–AV1.4 report noted the absence of integration and end-to-end tests. With all UI surfaces now hardened, this is the appropriate time to introduce MAUI UI test automation.
4. **Performance Profiling:** The dashboard loads all entities into memory for count aggregation. For large datasets, this should be replaced with `COUNT()` queries at the repository level.

---

**End of Report.**