using System;
using System.Security.Cryptography;
using System.Text;

namespace WorkGrid.Domain.Sync;

public sealed record SyncChange
{
    public Guid ChangeId { get; init; }
    public SyncObjectType ObjectType { get; init; }
    public Guid ObjectId { get; init; }
    public SyncOperation Operation { get; init; }
    public Guid OriginatingDeviceId { get; init; }
    public long SequenceNumber { get; init; }
    public string? Payload { get; init; }
    public string PayloadHash { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public SyncVersion Version => new(OriginatingDeviceId, SequenceNumber);

    public SyncChange(
        Guid changeId,
        SyncObjectType objectType,
        Guid objectId,
        SyncOperation operation,
        Guid originatingDeviceId,
        long sequenceNumber,
        string? payload,
        DateTimeOffset createdAt)
    {
        if (changeId == Guid.Empty)
            throw new ArgumentException("Change ID cannot be empty.", nameof(changeId));
        if (objectId == Guid.Empty)
            throw new ArgumentException("Object ID cannot be empty.", nameof(objectId));
        if (originatingDeviceId == Guid.Empty)
            throw new ArgumentException("Originating Device ID cannot be empty.", nameof(originatingDeviceId));
        if (sequenceNumber <= 0)
            throw new ArgumentException("Sequence number must be positive and greater than zero.", nameof(sequenceNumber));

        ChangeId = changeId;
        ObjectType = objectType;
        ObjectId = objectId;
        Operation = operation;
        OriginatingDeviceId = originatingDeviceId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
        CreatedAt = createdAt;
        PayloadHash = CalculateHash(payload ?? string.Empty);
    }

    public bool VerifyIntegrity()
    {
        return PayloadHash == CalculateHash(Payload ?? string.Empty);
    }

    private static string CalculateHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
