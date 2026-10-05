# ADR 0010: Synchronization Change and Version Model

## Status
Proposed & Approved (Foundation Sprints)

## Context
In WorkGrid Phase 5, synchronization requires describing mutations (creates, updates, and deletions) as discrete, identifiable, and immutable objects called **Changes**. We must determine:
1. How a change is uniquely identified.
2. How operations are classified.
3. How deletions are represented without losing history (tombstoning).
4. How versions are tracked to express causality and detect concurrent updates without relying on synchronized system clocks.

## Decisions

### 1. Change Identity
Every logical mutation is represented as a `SyncChange` record.
- **ChangeId**: A `Guid` generated at the point of origin. This guarantees global uniqueness and allows a receiver to ignore a duplicate change (idempotency constraint).

### 2. Operations and Tombstones
We define three operations:
- `Create`: The initial insertion of an entity.
- `Update`: Any subsequent modification.
- `Delete`: The removal of an entity. To ensure deletion propagates correctly to peer replicas (instead of being interpreted as missing data), we represent deletes via an explicit tombstone state (retaining the `ObjectId` and marking the operation as `Delete`).

### 3. Versioning and Causality (Logical Vector Clocks)
System clocks can drift or be altered on client devices. We choose a **Logical Sequence Model** backed by a logical Lamport timestamp concept combined with device identity:
- **SyncVersion**: Composed of `OriginatingDeviceId` (Guid) and a local `SequenceNumber` (long, monotonically increasing).
- Two versions are directly comparable if they originate from the same device: `(DeviceA, 5) > (DeviceA, 4)`.
- Concurrent or cross-device causality is determined during reconciliation (S5.3) using manifest differences.

### 4. Integrity and Payload
Each change includes a `PayloadHash` (SHA-256 string representation) to ensure data transmission integrity, and an optional `Payload` string (typically JSON) containing the serialized state.

## Consequences
- Every change is deterministic and self-contained.
- Duplicate transmissions are easily ignored using a simple hash-set or database index on `ChangeId`.
- No dependencies on system clock synchronization for ordering.
