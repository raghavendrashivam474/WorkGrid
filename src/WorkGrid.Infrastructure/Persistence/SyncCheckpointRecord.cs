using System;

namespace WorkGrid.Infrastructure.Persistence;

/// <summary>
/// Persistent record tracking the last successfully applied sequence
/// from a specific remote replica. One row per remote replica.
/// </summary>
public sealed class SyncCheckpointRecord
{
    public Guid RemoteReplicaId { get; set; }
    public long LastAppliedSequenceNumber { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
