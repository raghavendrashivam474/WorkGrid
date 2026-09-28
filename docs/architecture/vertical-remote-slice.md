# Architecture — Vertical Remote Data Slice (`S4.4`)

## 1. Overview

Sprint S4.4 introduces WorkGrid's first complete **vertical remote data slice**. It proves the entire authenticated remote communication chain from client application to server database without displacing local SQLite persistence or introducing premature synchronization complexity.

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                              CLIENT TIER                                │
│                                                                         │
│   WorkGrid.App ViewModels / UI                                          │
│                │                                                        │
│                ▼                                                        │
│   IEmployeeRemoteService (High-level data abstraction)                  │
│                │                                                        │
│                ▼                                                        │
│   IRemoteClient (HTTP transport, Bearer auth, timeout & retry)          │
└────────────────┼────────────────────────────────────────────────────────┘
                 │
                 │ HTTP GET /api/employees (Authorization: Bearer <token>)
                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              SERVER TIER                                │
│                                                                         │
│   WorkGrid.Api (Controllers / Middleware)                               │
│                │                                                        │
│                ▼ [Authorize] JWT Bearer Validation                      │
│   EmployeesController                                                   │
│                │                                                        │
│                ▼                                                        │
│   IEmployeeRepository (EF Core / WorkGridDbContext)                     │
│                │                                                        │
│                ▼                                                        │
│   Server Database (workgrid_server.db)                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

## 2. Data Flow & Boundary Isolation

To preserve clean boundaries, data transforms across three explicit shapes:

```text
Server Database ──► Domain Employee (Server)
                          │
                          ▼ ToResponse()
                     EmployeeResponse (API DTO)
                          │
                          ▼ JSON Serialization over HTTP
                     RemoteEmployeeDto (Client Transport DTO)
                          │
                          ▼ Constructor Mapping
                     Domain Employee (Client) ──► RemoteResult<IReadOnlyList<Employee>>
```

### Why Distinct DTOs?

- **API Isolation**: EmployeeResponse represents the server's public contract.
- **Client Transport Isolation**: RemoteEmployeeDto is private to WorkGrid.Infrastructure.Remote. If 
  the server modifies response serialization, changes are quarantined inside the remote service
  without affecting ViewModels.
- **Domain Purity**: Domain entities enforce business invariants and have zero dependencies on JSON or HTTP attributes.

## 3. Local-First Coexistence

The remote data path operates in parallel to local SQLite storage:
Here is the data formatted and properly indented as a Markdown table:

| Path | Primary Component | Storage / Endpoint | Authority |
| --- | --- | --- | --- |
| **Local Path** | `IEmployeeRepository` | Local SQLite (`workgrid.db3`) | Authoritative for offline operations |
| **Remote Path** | `IEmployeeRemoteService` | `WorkGrid.Api` (`/api/employees`) | Query channel for server-side state |


The mobile application remains fully operational offline. Remote queries return typed RemoteResult<T> envelopes that report transport failures gracefully without degrading local CRUD capabilities.

## 4. Error Classification in the Vertical Slice

| Failure Scenario | HTTP / Transport Event | RemoteResult.ErrorKind | Behavior |
| --- | --- | --- | --- |
| **Server down / Unreachable** | Socket exception after 2 retries | `RemoteErrorKind.Unreachable` | Error reported; UI stays on local data |
| **Request timeout** | Exceeded configured timeout | `RemoteErrorKind.Timeout` | Error reported; retry available |
| **Invalid / Expired Token** | HTTP 401 Unauthorized | `RemoteErrorKind.Unauthorized` | User prompted to re-authenticate |
| **Forbidden Access** | HTTP 403 Forbidden | `RemoteErrorKind.Unauthorized` | Permission denied message |
| **Server-side Crash** | HTTP 500 Internal Server Error | `RemoteErrorKind.HttpFailure` | Generic error; secrets masked |