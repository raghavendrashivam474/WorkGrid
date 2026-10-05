# Synchronization Domain Model

> **Phase:** 5 — Hybrid Synchronization Fabric
> **Sprint:** S5.1
> **Baseline:** v1.0.0
> **Status:** Defined
> **ADR:** Pending (see 0010 if material decisions emerge in S5.2)

---

## 1. What Synchronization Means in WorkGrid

Synchronization is the process by which two or more **replicas** of WorkGrid state
converge toward consistency by exchanging **changes** that occurred independently
on each replica.

Key principles:

- **Local-first:** The Android device's local SQLite database remains the
  authoritative application database for that device. Synchronization extends
  reach; it does not relocate authority.
- **Transport-agnostic:** The synchronization domain model is independent of
  whether changes travel via Direct (A), Relay (B), or Durable Cloud (C).
- **Eventual consistency:** Replicas may temporarily diverge. The reconciliation
  model (S5.3) defines how divergence is detected and resolved.
- **Non-destructive to existing architecture:** The Domain, Infrastructure, API,
  and App layers retain their current dependency directions and responsibilities.

---

## 2. Syncable Entity Classification

### 2.1 Employee

| Question | Decision |
|---|---|
| **Syncable?** | **Yes** |
| **Independently identifiable?** | Yes — `Guid Id` (globally unique) + `string EmployeeCode` (business key) |
| **References another entity?** | No direct FK references. Referenced *by* Assignment. |
| **Can be created offline?** | Yes — local-first operation is a v1.0.0 requirement |
| **Can be modified offline?** | Yes — `UpdateDetails(name, email, department?)` |
| **Can be deleted?** | Deferred — no `Delete()` method exists on the entity. Deletion semantics must be designed in S5.2. Current ADR-0002 defines deletion guard rules. |
| **Authorization affects replication?** | Yes — only users with appropriate `UserRole` should see employee records. Replication must respect tenant/role boundaries. |
| **Contains sensitive information?** | Moderate — `Email` is PII. `Name` and `Department` are organizational data. No financial or health data. |
| **Sync priority** | High — employees are referenced by assignments |

**Rationale:** Employees are core operational entities. A field technician on Device B
must see employees created on Device A to create assignments. Employee records are
relatively stable (low mutation rate), making them good candidates for early sync.

### 2.2 Asset

| Question | Decision |
|---|---|
| **Syncable?** | **Yes** |
| **Independently identifiable?** | Yes — `Guid Id` (globally unique) + `string AssetTag` (business key) |
| **References another entity?** | No direct FK references. Referenced *by* Assignment. |
| **Can be created offline?** | Yes |
| **Can be modified offline?** | Yes — `UpdateDetails(...)`, `MarkAssigned()`, `MarkAvailable()`, `MarkMaintenance()`, `Retire()` |
| **Can be deleted?** | Deferred — no `Delete()` method. Assets transition through `AssetStatus` lifecycle. `Retire()` is the terminal state. |
| **Authorization affects replication?** | Yes — role-based visibility |
| **Contains sensitive information?** | Low — `SerialNumber` is hardware-identifying but not PII. |
| **Sync priority** | High — assets are referenced by assignments |

**Rationale:** Assets are the other half of the assignment relationship. Status
transitions (`Available → Assigned → Returned → Maintenance → Retired`) are
frequent and must synchronize to prevent double-assignment conflicts.

### 2.3 Assignment

| Question | Decision |
|---|---|
| **Syncable?** | **Yes** |
| **Independently identifiable?** | Yes — `Guid Id` |
| **References another entity?** | **Yes** — `EmployeeId` (→ Employee) and `AssetId` (→ Asset) |
| **Can be created offline?** | Yes |
| **Can be modified offline?** | Yes — `CompleteReturn(returnedAt)` |
| **Can be deleted?** | Deferred — no `Delete()` method. Assignments complete via `CompleteReturn()`. |
| **Authorization affects replication?** | Yes — assignments are scoped to the user's operational context |
| **Contains sensitive information?** | Moderate — links employees to assets (operational data) |
| **Sync priority** | High — this is the core business operation |

**Rationale:** Assignments are the primary business transaction. They create the
highest conflict risk (two devices assigning the same asset simultaneously).
Synchronization must handle the `EmployeeId` and `AssetId` references correctly:
an assignment cannot be meaningfully applied on a replica that lacks the referenced
employee or asset.

**Relationship sync ordering:** Employees and Assets must be synchronized *before*
Assignments that reference them. The reconciliation model (S5.3) must account for
referential dependency ordering.

### 2.4 User

| Question | Decision |
|---|---|
| **Syncable?** | **Deferred** |
| **Independently identifiable?** | Yes — `Guid Id` + `string Username` |
| **References another entity?** | No |
| **Can be created offline?** | Technically yes, but administratively no — user creation is a privileged operation |
| **Can be modified offline?** | Partially — `RecordLogin()`, `UpdatePasswordHash()` are local-only concerns |
| **Can be deleted?** | No — `Deactivate()` is the terminal operation |
| **Authorization affects replication?** | **Critically** — User contains `PasswordHash`, `Role`, and `IsActive`. Replicating user records across devices creates severe security surface. |
| **Contains sensitive information?** | **High** — `PasswordHash` is a credential. `Role` determines authorization. |
| **Sync priority** | **Deferred to a later phase** |

**Rationale for deferral:** User records are fundamentally different from operational
entities. `PasswordHash` must never be replicated to other devices via the sync
protocol. `Role` changes are administrative and should flow through the API's
authentication/authorization boundary, not through peer-to-peer sync. User identity
is already handled by the existing authentication subsystem (ADR-0008). Synchronizing
User entities would conflate *identity management* with *operational data sync*,
violating separation of concerns.

**Decision:** User synchronization is explicitly **out of scope** for Phase 5 S5.1–S5.3.
If future requirements demand multi-device user provisioning, it will be addressed
as a separate architectural concern with its own ADR.

---

## 3. Non-Syncable / Deferred Entities

| Entity | Status | Reason |
|---|---|---|
| `User` | **Deferred** | Security-sensitive credentials; identity management ≠ operational sync |
| Any future `AuditLog` entity | **Deferred** | Append-only; may require separate replication strategy |
| Any future `SyncMetadata` entity | **N/A** | Will be part of the sync infrastructure, not operational state |

---

## 4. Object Identity

Every synchronizable piece of WorkGrid state is uniquely identified by the
combination of:

```text
ObjectIdentity = (ObjectType, ObjectId)
```

Where:
- **ObjectType** is a stable discriminator: `"Employee"`, `"Asset"`, `"Assignment"`
- **ObjectId** is the entity's existing `Guid Id`

**Why not ObjectId alone?** While `Guid` values are globally unique and collisions
are astronomically unlikely, the ObjectType discriminator provides:
1. Explicit routing during reconciliation (different entity types may have
   different merge strategies)
2. Protection against theoretical cross-type ID collisions
3. Clear manifest structure (S5.3)

**Critical constraint:** The existing `Guid Id` on each entity is generated at
creation time (client-side for local-first). This means two devices creating the
*same logical entity* independently will produce **different Guids**. This is a
known challenge that the change model (S5.2) and reconciliation (S5.3) must address.
Potential strategies include:
- Business-key deduplication (`EmployeeCode`, `AssetTag`)
- Server-assigned canonical IDs (via Path C)
- Deterministic ID generation from business keys

**No decision is made here.** S5.1 documents the problem; S5.2/S5.3 will resolve it.

---

## 5. Device Identity

Synchronization requires distinguishing **which replica** produced a change,
independently from **which user** performed the action.

### Conceptual Model

```text
DeviceIdentity
├── DeviceId : Guid (stable, generated once per device installation)
├── DeviceLabel : string (human-readable, e.g., "Raghav's Pixel 8")
└── DeviceType : enum (Android, Windows, Server, etc.)
```

### Distinction from User Identity

| Concept | Represents | Example | Managed by |
|---|---|---|---|
| **User Identity** | *Who* performed the operation | User "Raghav" (Role: Admin) | Existing Auth subsystem (ADR-0008) |
| **Device Identity** | *Which replica* holds/produced the state | Device "Pixel-8-abc123" | Sync subsystem (this model) |

A single user may operate multiple devices. A single device may be used by
multiple users (though WorkGrid's current model is single-user-per-session).

**S5.1 scope:** Define the concept only. Device registration, persistence of
`DeviceId`, and device lifecycle management belong to later sprints.

---

## 6. Replica Identity

A **replica** is a particular device's synchronized view of WorkGrid state.

```text
ReplicaIdentity
├── ReplicaId : Guid (unique per replica; may equal DeviceId for 1:1)
├── DeviceId : Guid (the physical device)
├── UserId : Guid (the user whose data this replica holds)
└── SyncCheckpoint : (defined in S5.3)
```

**Why separate ReplicaId from DeviceId?** A device could theoretically host
multiple replicas (e.g., different user profiles, or a reset that creates a
new replica lineage). For Phase 5, a 1:1 mapping is expected, but the model
should not preclude future flexibility.

---

## 7. Ownership / Authorization Boundary

Synchronization must respect the existing authorization model (ADR-0005):

- **Role-based visibility:** A replica should only receive sync data for
  entities the local user is authorized to access.
- **No privilege escalation via sync:** A device operated by a read-only user
  must not receive admin-level entity mutations through the sync channel.
- **Authorization is evaluated at the transport boundary**, not within the
  sync domain model itself. The sync model defines *what* can sync; the
  transport layer enforces *who* may sync it.

---

## 8. Local Authority Principle

**The local SQLite database on each Android device remains the authoritative
source of truth for that device's operations.**

This means:
1. All CRUD operations execute against local state first.
2. Synchronization is a *background convergence process*, not a prerequisite
   for operation.
3. If sync is unavailable, the device continues to function fully.
4. Conflicts are resolved according to rules defined in S5.3, but the local
   device's operations are never silently discarded.
5. The cloud (Path C) may serve as a coordination point or durable backup,
   but it does not override local authority by default.

---

## 9. Relationship Considerations

The existing entity graph is:

```text
Employee (1) ──── (N) Assignment (N) ──── (1) Asset
```

Synchronization implications:

1. **Referential integrity across replicas:** An Assignment referencing
   `EmployeeId=X` and `AssetId=Y` is meaningless on a replica that has not
   yet received Employee X or Asset Y.
2. **Sync ordering:** The reconciliation process must handle dependency
   ordering — sync Employees and Assets before Assignments.
3. **Orphan detection:** If an Assignment references an Employee that was
   deleted on another replica, the reconciliation model must define the
   outcome (reject, tombstone, re-parent).
4. **Cascade semantics:** The current domain has no cascade delete. Sync
   must not introduce implicit cascades.

---

## 10. Explicit Exclusions

The following are **explicitly out of scope** for S5.1 and the broader
S5.1–S5.3 foundation:

| Exclusion | Reason |
|---|---|
| User entity synchronization | Security; deferred (see §2.4) |
| Transport implementation (A/B/C) | S5.4+ |
| Encryption / compression pipeline | Post-protocol |
| CRDT data structures | Architectural decision not yet justified |
| Event sourcing | Current persistence model preserved |
| CQRS / MediatR | Not required for sync foundation |
| Real-time push notifications | Transport concern |
| Conflict auto-resolution UI | Application-layer concern |
| Cloud database schema | Path C implementation |
| Device pairing / discovery | Path A implementation |

---

## Appendix: Entity Summary Table

| Entity | Sync? | ID Type | Business Key | Offline Create | Offline Modify | Delete | Relationships | Sensitivity |
|---|---|---|---|---|---|---|---|---|
| Employee | ✅ Yes | Guid | EmployeeCode | ✅ | ✅ | ⚠️ Deferred | ← Assignment | Moderate (PII) |
| Asset | ✅ Yes | Guid | AssetTag | ✅ | ✅ | ⚠️ Deferred (Retire) | ← Assignment | Low |
| Assignment | ✅ Yes | Guid | — | ✅ | ✅ | ⚠️ Deferred | → Employee, → Asset | Moderate |
| User | ❌ Deferred | Guid | Username | ⚠️ Admin only | ⚠️ Partial | ❌ Deactivate | — | **High** (credentials) |
