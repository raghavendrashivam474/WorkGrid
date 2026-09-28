# AV1.1 Android Baseline & Device Audit Report

- **Baseline Milestone**: v4.0
- **Verification Date**: 2026-09-28 23:56:19
- **Status**: Completed (Inspection & Verification)

---

## 1. System Topology & Path Diagnostics

- **Mobile Client Database Path**:
  - FileSystem.AppDataDirectory/workgrid.db3 (Configured in src/WorkGrid.App/MauiProgram.cs)
- **Remote API Server Database Path**:
  - AppContext.BaseDirectory/workgrid_server.db (Configured in src/WorkGrid.Api/Program.cs)
- **Client/Server Architectural Separation**:
  - Confirmed distinct physical SQLite connections.
  - Client connections configured in WorkGrid.Infrastructure using Entity Framework Core SQLite options.

---

## 2. Navigation & Route Alignment Matrix

All Views paired successfully to ViewModels inside the dependency injection layer.

### Declared Shell Routes (AppShell.xaml)
- login
- main
- home
- employees
- ssets
- ssignments
- users

### Programmatic Modal/Detail Routes (AppShell.xaml.cs)
- employee-detail -> EmployeeDetailPage
- sset-detail -> AssetDetailPage
- ssignment-detail -> AssignmentDetailPage

---

## 3. Verified Build & Test Suite Registry

- **Build Quality**: 0 Errors, 0 Warnings
- **Test Metrics**:
  - Total Passing Tests: **120**
  - Failures: **0**
  - Regressions: **None**

---

## 4. Operational Defect Classification (Initial Audit)

Below is the verified inventory compiled for upcoming sprints:

| ID | Module / Feature | Issue Description | Severity | Target Sprint |
| :--- | :--- | :--- | :--- | :--- |
| **D-101** | App Startup / Auth | Lack of proactive/reactive authentication state check; possibility of direct nav bypass if deep-linked. | **P1** | AV1.2 |
| **D-102** | App Startup / Auth | Missing double-tap submission guards on Login button. | **P2** | AV1.2 |
| **D-103** | Employees | Input boundary checking missing on CRUD detail screens (Employee Code duplicates). | **P1** | AV1.3 |
| **D-104** | Assets | Missing UI-level validation preventing invalid lifecycle transitions (e.g., Assigned directly to Retire). | **P1** | AV1.4 |

---

## 5. Architectural Guardrails Acceptance
- **No changes made to existing infrastructure classes during AV1.1.**
- Local database architecture remains authoritative.
- Network dependency has not been introduced into base startup lifecycle.
