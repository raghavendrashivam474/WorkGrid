using System;

namespace WorkGrid.Domain.Sync;

public sealed record SyncVersion(Guid OriginatingDeviceId, long SequenceNumber)
{
    public bool IsNewerThan(SyncVersion other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return OriginatingDeviceId == other.OriginatingDeviceId && SequenceNumber > other.SequenceNumber;
    }

    public bool IsOlderThan(SyncVersion other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return OriginatingDeviceId == other.OriginatingDeviceId && SequenceNumber < other.SequenceNumber;
    }
}
