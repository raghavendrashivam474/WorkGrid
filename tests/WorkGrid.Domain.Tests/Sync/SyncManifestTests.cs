using System;
using System.Collections.Generic;
using Xunit;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Domain.Tests.Sync;

public class SyncManifestTests
{
    [Fact]
    public void Reconcile_ShouldCorrectlyIdentifyAlreadyKnownObjects()
    {
        var localId = Guid.NewGuid();
        var remoteId = Guid.NewGuid();
        var objectId = Guid.NewGuid();
        var origDevice = Guid.NewGuid();

        var key = new SyncObjectKey(SyncObjectType.Employee, objectId);
        var version = new SyncVersion(origDevice, 42);
        var summary = new SyncObjectSummary(key, version, "hash-matching");

        var localManifest = new SyncManifest(localId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, summary } });
        var remoteManifest = new SyncManifest(remoteId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, summary } });

        var results = SyncReconciler.Reconcile(localManifest, remoteManifest);

        var result = Assert.Single(results);
        Assert.Equal(key, result.Key);
        Assert.Equal(ReconciliationStatus.AlreadyKnown, result.Status);
    }

    [Fact]
    public void Reconcile_ShouldIdentifyMissingLocallyAndRemotely()
    {
        var localId = Guid.NewGuid();
        var remoteId = Guid.NewGuid();
        var keyL = new SyncObjectKey(SyncObjectType.Asset, Guid.NewGuid());
        var keyR = new SyncObjectKey(SyncObjectType.Assignment, Guid.NewGuid());

        var sumL = new SyncObjectSummary(keyL, new SyncVersion(localId, 1), "hash-l");
        var sumR = new SyncObjectSummary(keyR, new SyncVersion(remoteId, 1), "hash-r");

        var localManifest = new SyncManifest(localId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { keyL, sumL } });
        var remoteManifest = new SyncManifest(remoteId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { keyR, sumR } });

        var results = new List<ReconciliationResult>(SyncReconciler.Reconcile(localManifest, remoteManifest));

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Key == keyL && r.Status == ReconciliationStatus.MissingRemotely);
        Assert.Contains(results, r => r.Key == keyR && r.Status == ReconciliationStatus.MissingLocally);
    }

    [Fact]
    public void Reconcile_ShouldDetectDivergencesAndNewerVersions()
    {
        var localId = Guid.NewGuid();
        var remoteId = Guid.NewGuid();
        var origDevice = Guid.NewGuid();
        var key = new SyncObjectKey(SyncObjectType.Employee, Guid.NewGuid());

        // Scenario 1: Remote is newer
        var sumL1 = new SyncObjectSummary(key, new SyncVersion(origDevice, 10), "hash-old");
        var sumR1 = new SyncObjectSummary(key, new SyncVersion(origDevice, 15), "hash-new");

        var lm1 = new SyncManifest(localId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, sumL1 } });
        var rm1 = new SyncManifest(remoteId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, sumR1 } });

        var res1 = Assert.Single(SyncReconciler.Reconcile(lm1, rm1));
        Assert.Equal(ReconciliationStatus.NewerRemotely, res1.Status);

        // Scenario 2: Unrelated paths (different originating devices editing same object) -> Conflict
        var sumL2 = new SyncObjectSummary(key, new SyncVersion(localId, 5), "hash-edit-local");
        var sumR2 = new SyncObjectSummary(key, new SyncVersion(remoteId, 5), "hash-edit-remote");

        var lm2 = new SyncManifest(localId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, sumL2 } });
        var rm2 = new SyncManifest(remoteId, new Dictionary<SyncObjectKey, SyncObjectSummary> { { key, sumR2 } });

        var res2 = Assert.Single(SyncReconciler.Reconcile(lm2, rm2));
        Assert.Equal(ReconciliationStatus.PotentialConflict, res2.Status);
    }

    [Fact]
    public void Checkpoint_ShouldEnforceInvariants()
    {
        var remoteReplica = Guid.NewGuid();

        // Valid
        var checkpoint = new SyncCheckpoint(remoteReplica, 100);
        Assert.Equal(remoteReplica, checkpoint.RemoteReplicaId);
        Assert.Equal(100, checkpoint.LastAppliedSequenceNumber);

        // Invalid
        Assert.Throws<ArgumentException>(() => new SyncCheckpoint(Guid.Empty, 100));
        Assert.Throws<ArgumentException>(() => new SyncCheckpoint(remoteReplica, -1));
    }
}
