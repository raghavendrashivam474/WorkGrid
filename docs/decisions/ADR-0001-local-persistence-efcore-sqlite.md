# ADR-0001: Local Persistence with EF Core and SQLite

## Status
Accepted

## Context
WorkGrid requires a durable, local-first database to store and query operational entities (`Employee`, `Asset`, `Assignment`) completely offline on Android mobile devices.

## Decision
We adopted **Microsoft.EntityFrameworkCore.Sqlite** (version 8.0.x) strictly within `WorkGrid.Infrastructure`.

Key aspects of this decision:
1. **Purity of Domain:** `WorkGrid.Domain` maintains zero references to EF Core or SQLite. Domain contracts (`IEmployeeRepository`, `IAssetRepository`, `IAssignmentRepository`) define persistence needs.
2. **Explicit Repositories:** We rejected generic repository abstractions (`IRepository<T>`) in favor of dedicated, strongly-typed repository contracts tailored to domain operations.
3. **Storage Location:** On mobile devices, the SQLite database is stored in `FileSystem.AppDataDirectory` (`workgrid.db3`).
4. **Schema Initialization:** During mobile startup, `Database.EnsureCreated()` initializes the local database schema deterministically.

## Consequences
- **Positive:** Full local offline capability; entities and state transitions survive application restarts; compile-time safety on queries.
- **Negative:** EF Core adds a minimal runtime binary overhead to the infrastructure layer, which is acceptable for modern mobile devices.
