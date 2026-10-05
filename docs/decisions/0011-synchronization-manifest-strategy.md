# ADR 0011: Synchronization Manifest and Reconciliation Strategy

## Status
Proposed & Approved (Foundation Sprints)

## Context
We need a way for two WorkGrid replicas (e.g., an Android client and a Cloud relay or Peer device) to exchange their current synchronization states without transmitting the entire database. They must be able to:
1. Determine what data is identical.
2. Identify missing data in either direction.
3. Detect modifications and identify conflicts.
4. Keep track of synchronization checkpoints safely so that failures do not result in incomplete synchronization records.

## Decisions

### 1. The Sync Manifest Structure
We choose a flat, lightweight **Sync Manifest** structure. A manifest represents the state of a replica at a specific point in time:
- It contains a `ReplicaId` identifying the source of the manifest.
- It maps unique `SyncObjectKey` records to `SyncObjectSummary` records containing version sequence and state hashes.

```text
SyncObjectKey = (SyncObjectType, Guid ObjectId)
SyncObjectSummary = (SyncObjectKey, SyncVersion, StateHash)
```

### 2. Reconciliation Engine
We implement a pure, in-memory **Reconciliation Engine** that takes a Local Manifest and a Remote Manifest and outputs a set of `ReconciliationResult` records.
The possible statuses match our synchronization requirements:
- `AlreadyKnown`: Both replicas have the exact same version and state hash.
- `MissingLocally`: The remote replica has an object that the local replica does not have.
- `MissingRemotely`: The local replica has an object that the remote replica does not have.
- `NewerLocally`: The local replica has a newer version of the same object (same originating device, higher sequence number).
- `NewerRemotely`: The remote replica has a newer version of the same object (same originating device, higher sequence number).
- `PotentialConflict`: Both replicas have updated the same object independently, resulting in divergent histories or concurrent sequences.

### 3. Checkpointing Semantics
To prevent starting synchronization from scratch each time, we define a transactional `SyncCheckpoint` record:
- Tracks the last successfully processed sequence number for a given remote replica.
- Stored locally on the client. It is updated only *after* the corresponding batch of changes has been successfully written to local persistence.

### 4. Transport Independence
No transport-specific concerns (IP addresses, Cloud Relay IDs, HTTP codes) are permitted in this domain layer. It remains pure mathematical comparison.

## Consequences
- Efficient synchronization protocol: replicas only request the exact missing `SyncChange` records identified during reconciliation.
- Complete protection against clock-skew issues by relying strictly on causal logical sequence numbers and cryptographic hashes.
