# ADR 0004: Remote Boundary Foundation (S4.1)

## Context
WorkGrid baseline v3.0 operates as a local-first application backed by SQLite and EF Core. Phase 4 introduces the foundations for future multi-node / API communication. Before full synchronization or remote business operations are introduced, the application requires an isolated, non-intrusive remote boundary to communicate over HTTP/HTTPS, represent connectivity states, handle transient transport failures, and manage endpoint configuration without compromising local-first guarantees.

## Decision
1. **Infrastructure Isolation**: All remote HTTP communication types reside in WorkGrid.Infrastructure.Remote. The Domain layer is untouched.
2. **Abstractions and Models**:
   - RemoteEndpoint: Immutable value object validating absolute HTTP/HTTPS URIs and custom timeouts.
   - ConnectionState: 5-state lifecycle model (Unknown, Connecting, Connected, Disconnected, Recovering).
   - RemoteErrorKind: Transport-level categorisation (Timeout, Unreachable, HttpFailure, Unauthorized, Cancelled, Unknown).
   - RemoteResult: Result envelope separating transport status from domain errors.
   - IRemoteClient / RemoteClient: Encapsulates HttpClient, cancellation token management, timeout mapping, and bounded transient retry recovery.
3. **Local-First Preservation**:
   - IRemoteClient is registered in DI with a default endpoint (https://localhost:5001), maintaining full backwards compatibility.
   - No existing ViewModels or local SQLite repositories depend on network availability to function.

## Status
Accepted

## Consequences
- **Positive**: Clear separation of transport networking from UI and Domain layers.
- **Positive**: Zero changes required for existing 88 baseline tests or ViewModels.
- **Positive**: Fully testable via standard HttpMessageHandler injection without live network access.
- **Trade-off**: High-level synchronization, change tracking, and server contract mapping are deferred to subsequent Phase 4 sprints.
