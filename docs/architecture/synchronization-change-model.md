# Synchronization Change Model

> **Sprint:** S5.2
> **Status:** Documented and Implemented (Domain Primitives)

## Change Structure

A `SyncChange` contains all the operational metadata required to apply a state change onto another replica:

| Property | Type | Description |
|---|---|---|
| `ChangeId` | `Guid` | Globally unique ID generated at the source device. Supports idempotency. |
| `ObjectType` | `SyncObjectType` | `Employee`, `Asset`, or `Assignment`. |
| `ObjectId` | `Guid` | The target domain entity's ID. |
| `Operation` | `SyncOperation` | `Create`, `Update`, or `Delete`. |
| `OriginatingDeviceId` | `Guid` | Identifies which physical replica generated this change. |
| `SequenceNumber` | `long` | Monotonically increasing counter on the originating device. |
| `Payload` | `string?` | Serialized JSON representation of changed fields or full entity state. |
| `PayloadHash` | `string` | Hash of the payload for integrity and verification. |
| `CreatedAt` | `DateTimeOffset` | Wall-clock timestamp for audit and user interface display (not used for logical ordering). |

## Version Comparison and Causality

To determine whether a change $C_1$ is newer than $C_2$:
1. If $C_1.\text{OriginatingDeviceId} == C_2.\text{OriginatingDeviceId}$:
   - Compare `SequenceNumber` values directly. The larger sequence number is strictly newer.
2. If they originate from different devices, they are considered *concurrent* at the change level until reconciled against a replica's known manifest (S5.3).
