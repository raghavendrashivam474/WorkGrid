# ADR-0009: Application-Facing Remote Service Boundary and DTO Translation

## Status
Accepted (S4.4)

## Context

With `WorkGrid.Api` exposing authenticated REST endpoints (`S4.2`) and `IRemoteClient` handling HTTP transport and JWT token management (`S4.3`), the application requires a high-level service to consume remote data.

Directly using `IRemoteClient` in ViewModels or UI components would force them to:
- Construct URL strings (e.g. `/api/employees`)
- Manage raw JSON deserialization
- Convert API DTO structures into Domain models
- Handle low-level HTTP status codes

At the same time, this remote capability must **not** replace the existing local SQLite repository (`IEmployeeRepository`), nor should it introduce an unneeded bidirectional synchronization engine.

## Decision

1. **High-Level Service Abstraction:** Created `IEmployeeRemoteService` in `WorkGrid.Infrastructure.Remote` with `Task<RemoteResult<IReadOnlyList<Employee>>> GetEmployeesAsync(...)`.
2. **Infrastructure DTO Isolation:** Created `RemoteEmployeeDto` as an internal record within `WorkGrid.Infrastructure.Remote` to deserialize API payloads without exposing API serialization schemas to ViewModels or Domain.
3. **Generic Result Envelope:** Extended `RemoteResult` with `RemoteResult<T>` to carry strongly-typed payloads while preserving transport error classifications (`Timeout`, `Unreachable`, `Unauthorized`).
4. **Domain Entity Construction:** `EmployeeRemoteService` maps `RemoteEmployeeDto` instances into valid `WorkGrid.Domain.Entities.Employee` objects before returning.
5. **Local-First Preservation:** `IEmployeeRepository` remains the local-first authority for device persistence. `IEmployeeRemoteService` acts as an independent remote query channel.

```text
┌─────────────────────────────────────────────────────────────┐
│                       WorkGrid.App                          │
│                                                             │
│   ┌───────────────────────────┐ ┌────────────────────────┐  │
│   │    IEmployeeRepository    │ │ IEmployeeRemoteService │  │
│   │     (Local SQLite)        │ │    (Remote Query)      │  │
│   └─────────────┬─────────────┘ └───────────┬────────────┘  │
└─────────────────┼───────────────────────────┼───────────────┘
                  │                           │
                  ▼                           ▼
            SQLite Database             IRemoteClient
                                              │ HTTP / Bearer
                                              ▼
                                         WorkGrid.Api
```

## Rationale

1. **Separation of Concerns**: ViewModels interact with clean C# interfaces returning 
   `RemoteResult<IReadOnlyList<Employee>>` rather than dealing with HTTP or JSON.
2. **No Synchronisation Leakage**: This is a read capability, not a synchronization engine. Keeping it as
   a distinct remote service prevents premature complexity (conflict resolution, dirty flags, outboxes).
3. **Domain Independence**: `WorkGrid.Domain` remains pure and unaware of HTTP or API serialization.
4. **Resilience**: If the remote API is unreachable or times out, the failure is reported cleanly in 
   `RemoteResult<T>`, leaving local SQLite operations completely functional.

## Consequences

- Application layers can query remote data explicitly when connectivity is desired.
- Future synchronization capabilities (Phase 5) can build upon `IEmployeeRemoteService`
  and `IRemoteClient` without modifying existing local repositories.

## Related

- **ADR-0006**: Remote Boundary Foundation (S4.1)
- **ADR-0007**: Server-Side Persistence Strategy (S4.2)
- **ADR-0008**: Stateless JWT Bearer Authentication (S4.3)

S4.4 Closeout Report
