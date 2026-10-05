using System;

namespace WorkGrid.Infrastructure.Persistence;

/// <summary>
/// Persistent record of a single local synchronization change.
/// Serves as both the change log and the tombstone store for deleted objects.
/// Maps to the domain SyncChange type but lives in Infrastructure for EF Core persistence.
/// </summary>
public sealed class SyncChangeRecord
{
    public Guid ChangeId { get; set; }
    public int ObjectType { get; set; }
    public Guid ObjectId { get; set; }
    public int Operation { get; set; }
    public Guid OriginatingDeviceId { get; set; }
    public long SequenceNumber { get; set; }
    public string? Payload { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
