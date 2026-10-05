using System;

namespace WorkGrid.Domain.Sync;

public sealed record SyncCheckpoint
{
    public Guid RemoteReplicaId { get; init; }
    public long LastAppliedSequenceNumber { get; init; }

    public SyncCheckpoint(Guid remoteReplicaId, long lastAppliedSequenceNumber)
    {
        if (remoteReplicaId == Guid.Empty)
            throw new ArgumentException("Remote Replica ID cannot be empty.", nameof(remoteReplicaId));
        if (lastAppliedSequenceNumber < 0)
            throw new ArgumentException("Last applied sequence number cannot be negative.", nameof(lastAppliedSequenceNumber));

        RemoteReplicaId = remoteReplicaId;
        LastAppliedSequenceNumber = lastAppliedSequenceNumber;
    }
}
