# WorkGrid — Architecture Overview

## 1. Purpose & Vision

WorkGrid is designed as a hybrid, local-first platform for managing employees, equipment, and asset assignments in operational environments.

### Core Principles
1. **Local-First:** The mobile client operates autonomously without requiring an active internet connection. All critical read/write operations execute against a local SQLite store.
2. **Hybrid Synchronization (Future):** When connectivity is available, the mobile client synchronizes state with an ASP.NET Core backend.
3. **Clean Separation of Concerns:** Domain logic remains completely decoupled from UI frameworks, persistence mechanisms, and network concerns.

---

## 2. Current State (S0.2 / S0.3) vs Planned Evolution

### CURRENT (Phase 0 / S0.2–S0.3)
* **Solution Structure:** Standardized .NET 8 solution containing 4 primary projects (WorkGrid.App, WorkGrid.Domain, WorkGrid.Infrastructure, WorkGrid.Api) and 3 test projects.
* **Dependency Direction:** Established, reviewed, and enforced via project references (see Dependency Policy Matrix below).
* **WorkGrid.App:** MVVM-structured .NET MAUI application targeting Android with ViewModelBase, MainViewModel, Views/, ViewModels/, Services/ organization, and constructor-based DI via MauiProgram.cs.
* **WorkGrid.Domain:** Pure C# domain model containing Employee, Asset, and Assignment entities with business invariants, AssetStatus/AssignmentStatus enums, and DomainValidationException. Covered by 23 unit tests. Zero framework coupling.
* **WorkGrid.Infrastructure:** Organized with Persistence/, Repositories/, Services/, DependencyInjection/ directories. No persistence implementation yet — reserved for Phase 1.
* **WorkGrid.Api:** Clean ASP.NET Core minimal baseline, structurally reserved for Phase 4.

### PLANNED (Future Phases)
* **Phase 1 (v1.0):** SQLite persistence via EF Core, local CRUD repositories, and core mobile MVVM screens for Employee/Asset/Assignment workflows.
* **Phase 2 (v2.0):** Asset checkout/checkin workflows, maintenance tracking, assignment history.
* **Phase 3 (v3.0):** Input validation, identity, logging, unit & integration test coverage expansion.
* **Phase 4 (v4.0):** ASP.NET Core backend endpoints, server database integration (PostgreSQL / SQL Server).
* **Phase 5 (v5.0):** Bidirectional delta synchronization engine with conflict resolution.

---

## 3. Dependency Rules & Direction

The solution strictly enforces the following architectural dependency hierarchy:

```text
                    WorkGrid.App
                    /           \
                   /             \
                  ▼               ▼
        WorkGrid.Domain    WorkGrid.Infrastructure
                                  │
                                  ▼
                           WorkGrid.Domain
And for the future backend:
```
```text
                    WorkGrid.Api
                    /          \
                   ▼            ▼
          WorkGrid.Domain   WorkGrid.Infrastructure
                                  │
                                  ▼
                           WorkGrid.Domain
```

### Dependency Policy Matrix

| Project | May Depend On | Must NEVER Depend On |
| --- | --- | --- |
| **WorkGrid.Domain** | None (Pure C# / .NET standard types) | WorkGrid.App, WorkGrid.Infrastructure, WorkGrid.Api, UI or DB packages |
| **WorkGrid.Infrastructure** | WorkGrid.Domain | WorkGrid.App, WorkGrid.Api |
| **WorkGrid.App** | WorkGrid.Domain, WorkGrid.Infrastructure | WorkGrid.Api (Client must not reference API project directly) |
| **WorkGrid.Api** | WorkGrid.Domain, WorkGrid.Infrastructure | WorkGrid.App |

## 4. Layer Responsibilities

### WorkGrid.Domain

- Core business entities (e.g. Employee, Asset)
- Domain value objects, enums, domain rules, and validation contracts
- Repository and service abstractions

### WorkGrid.Infrastructure

- Technical persistence implementations (EF Core, SQLite contexts)
- File storage, device-specific infrastructure adapters
- Future network & sync communication handlers

### WorkGrid.App

- Presentation layer (.NET MAUI, XAML)
- ViewModels (MVVM) and navigation state
- Platform-specific mobile UX adapters

### WorkGrid.Api

- REST / HTTP API endpoints
- Authentication & authorization
- Server-side orchestration and synchronization endpoints


