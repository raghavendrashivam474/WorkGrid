# ADR-0007: Server-Side Persistence Reuses Infrastructure Layer

## Status
Accepted (S4.2)

## Context

S4.2 introduces `WorkGrid.Api` as an ASP.NET Core host. The API needs
server-side data persistence to serve HTTP requests.

The existing `WorkGrid.Infrastructure` layer already contains:
- `WorkGridDbContext` (EF Core)
- Repository implementations (`EmployeeRepository`, etc.)
- `ServiceCollectionExtensions.AddInfrastructure()`

The mobile app uses the same Infrastructure layer with a local SQLite
database file on the device.

The question: should the API reuse the existing Infrastructure layer,
or should a separate server-side persistence stack be created?

## Decision

The API reuses `WorkGrid.Infrastructure` and its `AddInfrastructure()`
composition root, but with a **separate database file**
(`workgrid_server.db`) distinct from the mobile client's local database.

```text
Mobile App                    WorkGrid.Api
    │                              │
    ▼                              ▼
AddInfrastructure(            AddInfrastructure(
  "workgrid_local.db")          "workgrid_server.db")
    │                              │
    ▼                              ▼
  Same EF Core DbContext      Same EF Core DbContext
  Same repositories           Same repositories
  Different data file         Different data file
```

## Rationale

1. **No duplication**. Creating a parallel server persistence stack
   would duplicate repository code, entity configurations, and service
   registrations with no immediate benefit.

2. **Domain stays clean**. WorkGrid.Domain remains unaware of whether
   it is being hosted by a mobile app or a web API. The dependency
   direction is preserved.

3. **Explicit separation**. The database file path is the only
   configuration difference. This makes the mobile/server boundary
   visible and intentional rather than hidden.

4. **Future flexibility**. If server-side persistence later requires
   a different database engine (e.g., PostgreSQL), the change is
   isolated to the AddInfrastructure() call in Program.cs and
   potentially a new EF Core provider — not a full rewrite.

## Consequences

- The API and mobile app share the same EF Core model and migrations.
  Schema changes affect both. This is acceptable while the data model
  is stable and will be revisited if server requirements diverge.

- The server database starts empty. Data seeding or synchronization
  (S4.4+) will populate it.

- No production deployment architecture is implied. This is a
  development-stage decision.

## Related

- **ADR-0006**: Remote Boundary Foundation (S4.1)
- S4.2 Closeout Report
