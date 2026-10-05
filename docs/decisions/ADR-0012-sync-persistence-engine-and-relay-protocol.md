# ADR-0012: Sync Persistence, Local Engine, and Cloud Relay Transport Protocol

## Status
Accepted

## Context
WorkGrid Phase 5 requires moving from theoretical synchronization domain primitives (S5.1–S5.3) to operational synchronization (S5.4–S5.6). The solution must guarantee:
1. Local-first authority: SQLite remains the single source of truth for the local device.
2. Atomicity: Business entity mutations and their corresponding sync change records must commit together in a single transaction.
3. Monotonic logical versioning: Monotonically increasing sequence numbers per originating device identity.
4. Deterministic reconciliation: Transport-independent and side-effect-free reconciliation using the existing \SyncReconciler\.
5. Idempotent application: Safe replay and retry of changes.
6. Ephemeral cloud relay: Network transport (Path B) without turning the cloud into an authoritative business database.

## Decision
1. **Change Capture via DbContext Interception (Option B)**:
   - \WorkGridDbContext\ intercepts \SaveChangesAsync\ and \SaveChanges\ to inspect the ChangeTracker.
   - Changes to \Employee\, \Asset\, and \Assignment\ generate corresponding \SyncChangeRecord\ entries atomically within the same transaction.
   - Hard deletions are captured before row removal to generate tombstone change records with null payload and target ID.
   - Monotonic sequence numbers are calculated per originating device.
   - Repositories and domain entities remain completely untouched (satisfying §46 guardrails).

2. **Transport-Independent Sync Engine**:
   - \ISyncEngine\ orchestrates manifest building, local change selection, and remote change application.
   - Enforces domain invariants and verifies referential integrity (e.g., rejecting assignments if referenced employee or asset does not exist).
   - Manages \SyncCheckpointRecord\ per remote replica.

3. **Cloud Relay Transport (Path B)**:
   - Separate \/api/sync\ endpoints for session registration, manifest exchange, envelope queuing, polling, and acknowledgements.
   - Ephemeral in-memory routing coordinator without storing domain state on the server.
   - \ISyncTransport\ abstraction allowing seamless swapping between Relay, Direct P2P, or Durable Cloud replicas in future sprints.

## Consequences
- **Positive**:
  - Full backward compatibility with v1.0.0 and existing migrations.
  - Zero coupling between domain entities and sync infrastructure.
  - 100% test coverage for restart persistence, sequence monotonicity, tombstones, atomicity, idempotency, and multi-replica relay convergence.
