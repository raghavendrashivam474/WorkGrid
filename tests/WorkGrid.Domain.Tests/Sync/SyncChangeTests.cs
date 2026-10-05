using System;
using Xunit;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Domain.Tests.Sync;

public class SyncChangeTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldInstantiateCorrectly()
    {
        var changeId = Guid.NewGuid();
        var objectId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var payload = "{\"Name\":\"Updated Employee\"}";
        var now = DateTimeOffset.UtcNow;

        var change = new SyncChange(
            changeId,
            SyncObjectType.Employee,
            objectId,
            SyncOperation.Update,
            deviceId,
            150,
            payload,
            now);

        Assert.Equal(changeId, change.ChangeId);
        Assert.Equal(SyncObjectType.Employee, change.ObjectType);
        Assert.Equal(objectId, change.ObjectId);
        Assert.Equal(SyncOperation.Update, change.Operation);
        Assert.Equal(deviceId, change.OriginatingDeviceId);
        Assert.Equal(150, change.SequenceNumber);
        Assert.Equal(payload, change.Payload);
        Assert.True(change.VerifyIntegrity());
    }

    [Fact]
    public void Constructor_WithEmptyIds_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SyncChange(
            Guid.Empty,
            SyncObjectType.Asset,
            Guid.NewGuid(),
            SyncOperation.Create,
            Guid.NewGuid(),
            1,
            null,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Version_ShouldBeDirectlyComparable_WhenDevicesMatch()
    {
        var deviceId = Guid.NewGuid();
        var v1 = new SyncVersion(deviceId, 10);
        var v2 = new SyncVersion(deviceId, 20);

        Assert.True(v2.IsNewerThan(v1));
        Assert.True(v1.IsOlderThan(v2));
        Assert.False(v1.IsNewerThan(v2));
    }

    [Fact]
    public void Version_ShouldBeIncomparable_WhenDevicesDiffer()
    {
        var v1 = new SyncVersion(Guid.NewGuid(), 10);
        var v2 = new SyncVersion(Guid.NewGuid(), 20);

        Assert.False(v2.IsNewerThan(v1));
        Assert.False(v1.IsNewerThan(v2));
    }

    [Fact]
    public void VerifyIntegrity_WithAlteredPayload_ShouldReturnFalse()
    {
        var change = new SyncChange(
            Guid.NewGuid(),
            SyncObjectType.Assignment,
            Guid.NewGuid(),
            SyncOperation.Create,
            Guid.NewGuid(),
            1,
            "original",
            DateTimeOffset.UtcNow);

        // Record with-expression mutation safely copies and tests structural integrity
        var tamperedChange = change with { Payload = "tampered" };
        Assert.False(tamperedChange.VerifyIntegrity());
    }
}
