# ADR-0003: EF Core Migration Strategy

## Context
WorkGrid v2.0 utilized `Database.EnsureCreated()` during application initialization. While suitable for initial prototyping, `EnsureCreated()` cannot apply incremental schema changes to existing databases without dropping tables, leading to data loss upon schema evolution.

Phase 3 introduces Identity and Authorization models requiring new database entities and tables. The persistence layer must support deterministic schema evolution while preserving local user data across application updates.

## Decision
1. Replace `Database.EnsureCreated()` with `Database.Migrate()` in `MauiProgram.cs` application startup.
2. Maintain standard EF Core Code-First migrations in `src/WorkGrid.Infrastructure/Persistence/Migrations`.
3. Provide `WorkGridDbContextFactory` implementing `IDesignTimeDbContextFactory<WorkGridDbContext>` for CLI tooling operations (`dotnet dotnet-ef`).
4. Pin `Microsoft.EntityFrameworkCore.Design` and `dotnet-ef` to version `8.0.11` matching `net8.0` target and existing `Microsoft.EntityFrameworkCore.Sqlite` version.

## Consequences
### Positive
- Schema migrations run automatically upon application launch without destroying existing data.
- Database versions are tracked deterministically in SQLite via `__EFMigrationsHistory`.
- Design-time tooling functions cleanly without requiring MAUI runtime container bootstrap.

### Negative / Operational Considerations
- Migrations must be scaffolded and checked into source control whenever model configurations change.
- SQLite column/table alter limitations require standard EF Core table rebuilds for certain destructive changes if required in the future.
