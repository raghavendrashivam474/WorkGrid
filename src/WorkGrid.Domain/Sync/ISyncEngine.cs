using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkGrid.Domain.Sync;

public interface ISyncEngine
{
    /// <summary>
    /// Generates a compact manifest of the local database state,
    /// including active entities and tombstones.
    /// </summary>
    Task<SyncManifest> GenerateLocalManifestAsync(CancellationToken ct = default);

    /// <summary>
    /// For a given list of keys, selects local changes starting after the remote's known version.
    /// </summary>
    Task<IReadOnlyList<SyncChange>> SelectLocalChangesAsync(
        IEnumerable<SyncObjectKey> keys,
        SyncManifest remoteManifest,
        CancellationToken ct = default);

    /// <summary>
    /// Processes and applies a list of incoming remote changes, maintaining 
    /// domain validation, referential integrity, and updating checkpoints.
    /// </summary>
    Task ApplyRemoteChangesAsync(
        Guid remoteReplicaId,
        IReadOnlyList<SyncChange> remoteChanges,
        CancellationToken ct = default);
}
