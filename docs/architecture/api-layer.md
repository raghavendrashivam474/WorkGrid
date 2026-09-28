# Architecture — API Layer (`WorkGrid.Api`)

## 1. Role and Position

`WorkGrid.Api` is an ASP.NET Core Web API host. It serves as the HTTP boundary between remote clients (such as the mobile `WorkGrid.App`) and server-side data/services.

```text
HTTP Clients (Mobile App / Future Web)
                 │
                 ▼
         ┌───────────────┐
         │  WorkGrid.Api │
         └───────┬───────┘
                 │
       ┌─────────┴─────────┐
       │                   │
       ▼                   ▼
WorkGrid.Domain   WorkGrid.Infrastructure
                           │
                           ▼
                    Server Database
```

## 2. Dependency Rules

The API layer adheres to the strict WorkGrid dependency rules:

1. `WorkGrid.Api` references `WorkGrid.Domain` and `WorkGrid.Infrastructure`.
2. `WorkGrid.Domain` has ZERO knowledge of `WorkGrid.Api`. No HTTP abstractions, ASP.NET 
   attributes, or JSON serialization attributes exist in Domain.
3. `WorkGrid.App` does not reference `WorkGrid.Api`. The mobile application interacts with remote 
   services exclusively over HTTP abstractions (`IRemoteClient`).

## 3. Explicit DTO Boundary

Domain entities are never returned directly from API endpoints.

```text
Domain Entity (Employee)
          │
          │  ToResponse() [Explicit Mapping]
          ▼
   DTO (EmployeeResponse)
          │
          │  JSON Serialization
          ▼
    HTTP Response
```

### Why DTOs?

- **Contract Stability**: Internal domain refactorings (e.g. adding domain methods, invariants, computed
   properties) do not accidentally break external HTTP consumers.
- **Security & Encapsulation**: Domain entities enforce business invariants via constructors and methods.
   DTOs are lightweight, immutable data containers (`record`) tailored for serialization.
- **No Mapping Framework Overhead**: Simple explicit extension methods (`ToResponse()`) are used 
   instead of heavy reflection-based mappers (e.g., AutoMapper), keeping startup and execution fast 
   and deterministic.

## 4. HTTP Conventions and Error Handling

Endpoints follow predictable status code conventions:

| Status Code | Condition | Example |
| --- | --- | --- |
| **200 OK** | Successful query / retrieval | `GET /api/employees` returns list |
| **400 Bad Request** | Client request syntax/validation failure | Invalid payload |
| **401 Unauthorized** | Missing or invalid authentication credential | Unauthenticated request |
| **403 Forbidden** | Authenticated user lacks required permission | Viewer requesting admin action |
| **404 Not Found** | Requested resource does not exist | Invalid ID |
| **499 Client Closed** | Request cancelled by caller | `OperationCanceledException` |
| **500 Server Error** | Unexpected server-side failure | Masked generic error response |

### Exception Masking

Internal system exceptions (e.g. SQLite locks, EF Core connection errors) are logged server-side via ILogger but never returned in raw HTTP response bodies. The API returns a safe JSON payload:

```JSON
{
  "error": "An unexpected error occurred while processing your request."
}
```

## 5. Non-Goals

`WorkGrid.Api` intentionally avoids:

- Business rule duplication (rules live in `Domain / Infrastructure` services).
- CQRS / MediatR complexity.
- Generic repository abstraction layers over EF Core.
- Dynamic query / OData complexity.
