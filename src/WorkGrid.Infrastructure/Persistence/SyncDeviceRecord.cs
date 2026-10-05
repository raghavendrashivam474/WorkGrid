using System;

namespace WorkGrid.Infrastructure.Persistence;

/// <summary>
/// Persistent record for the local device's stable identity.
/// Exactly one row exists in this table per database instance.
/// </summary>
public sealed class SyncDeviceRecord
{
    public Guid DeviceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
