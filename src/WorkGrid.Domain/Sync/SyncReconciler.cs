using System;
using System.Collections.Generic;

namespace WorkGrid.Domain.Sync;

public static class SyncReconciler
{
    public static IEnumerable<ReconciliationResult> Reconcile(SyncManifest local, SyncManifest remote)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(remote);

        var results = new List<ReconciliationResult>();
        var allKeys = new HashSet<SyncObjectKey>(local.Objects.Keys);
        allKeys.UnionWith(remote.Objects.Keys);

        foreach (var key in allKeys)
        {
            var hasLocal = local.Objects.TryGetValue(key, out var localSummary);
            var hasRemote = remote.Objects.TryGetValue(key, out var remoteSummary);

            if (hasLocal && !hasRemote)
            {
                results.Add(new ReconciliationResult(key, ReconciliationStatus.MissingRemotely, localSummary!.Version, null));
            }
            else if (!hasLocal && hasRemote)
            {
                results.Add(new ReconciliationResult(key, ReconciliationStatus.MissingLocally, null, remoteSummary!.Version));
            }
            else if (hasLocal && hasRemote)
            {
                var lVer = localSummary!.Version;
                var rVer = remoteSummary!.Version;

                if (lVer == rVer)
                {
                    if (localSummary.StateHash == remoteSummary.StateHash)
                    {
                        results.Add(new ReconciliationResult(key, ReconciliationStatus.AlreadyKnown, lVer, rVer));
                    }
                    else
                    {
                        results.Add(new ReconciliationResult(key, ReconciliationStatus.PotentialConflict, lVer, rVer));
                    }
                }
                else if (rVer.IsNewerThan(lVer))
                {
                    results.Add(new ReconciliationResult(key, ReconciliationStatus.NewerRemotely, lVer, rVer));
                }
                else if (lVer.IsNewerThan(rVer))
                {
                    results.Add(new ReconciliationResult(key, ReconciliationStatus.NewerLocally, lVer, rVer));
                }
                else
                {
                    results.Add(new ReconciliationResult(key, ReconciliationStatus.PotentialConflict, lVer, rVer));
                }
            }
        }

        return results;
    }
}
