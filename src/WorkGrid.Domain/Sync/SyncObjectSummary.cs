namespace WorkGrid.Domain.Sync;

public sealed record SyncObjectSummary(SyncObjectKey Key, SyncVersion Version, string StateHash);
