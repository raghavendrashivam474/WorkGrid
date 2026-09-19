# ADR-0002: Active Assignment Guard Rules for Entity Deletion

## Status
Accepted

## Context
In operational equipment tracking, deleting an employee who currently possesses assigned equipment or deleting an asset currently in the hands of an employee creates orphan states and violates auditability.

## Decision
We enforce a strict guard rule across the application and persistence boundary:
1. An `Employee` cannot be deleted if active assignments exist (`IAssignmentRepository.HasActiveAssignmentsForEmployeeAsync`).
2. An `Asset` cannot be deleted if active assignments exist (`IAssignmentRepository.HasActiveAssignmentsForAssetAsync`).
3. If deletion is attempted while active assignments exist, a descriptive error message is presented to the user and the transaction is aborted. All assigned equipment must be formally returned prior to deletion.

## Consequences
- **Positive:** Guarantees data integrity, prevents orphaned asset allocations, preserves audit history.
- **Negative:** Requires an extra asynchronous check query before deletion operations.
