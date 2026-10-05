namespace WorkGrid.Domain.Sync;

public enum ReconciliationStatus
{
    AlreadyKnown,
    MissingLocally,
    MissingRemotely,
    NewerLocally,
    NewerRemotely,
    PotentialConflict
}
