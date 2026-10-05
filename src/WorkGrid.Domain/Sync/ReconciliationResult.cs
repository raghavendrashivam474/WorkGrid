namespace WorkGrid.Domain.Sync;

public sealed record ReconciliationResult(
    SyncObjectKey Key,
    ReconciliationStatus Status,
    SyncVersion? LocalVersion,
    SyncVersion? RemoteVersion);
