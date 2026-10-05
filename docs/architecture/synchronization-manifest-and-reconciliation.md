# Synchronization Manifest & Reconciliation Model

> **Sprint:** S5.3
> **Status:** Documented and Implemented (Domain Primitives)

## State Reconciliation Workflow

```text
┌───────────────┐ ┌───────────────┐
│ Local Replica │ │ Remote Peer │
└───────┬───────┘ └───────┬───────┘
│ Get Manifest │
│─────────────────────────────────>│
│ │
│ Return Remote Manifest │
│<─────────────────────────────────│
│ │
│ ──┐ │
│ │ Reconcile() │
│ │ (Diff local vs remote) │
│ <─┘ │
│ │
│ Fetch specific changes │
│─────────────────────────────────>│
│ │
│ Apply and commit updates │
│ ──┐ │
│ │ Store Checkpoint │
│ <─┘ │
```

## Manifest Diff Rules

For any given `SyncObjectKey` present in either the local or remote manifest:

| Local State | Remote State | Logical Causality Rule | Resulting Status | Action Required |
|---|---|---|---|---|
| Absent | Present | Object exists only on Remote | `MissingLocally` | Download |
| Present | Absent | Object exists only on Local | `MissingRemotely` | Upload |
| Present ($V_L, H_L$) | Present ($V_R, H_R$) | $V_L == V_R$ and $H_L == H_R$ | `AlreadyKnown` | None |
| Present ($V_L, H_L$) | Present ($V_R, H_R$) | $V_R.\text{IsNewerThan}(V_L)$ | `NewerRemotely` | Download |
| Present ($V_L, H_L$) | Present ($V_R, H_R$) | $V_L.\text{IsNewerThan}(V_R)$ | `NewerLocally` | Upload |
| Present ($V_L, H_L$) | Present ($V_R, H_R$) | Unrelated origins or divergent hashes at same version | `PotentialConflict` | Conflict Resolution |
