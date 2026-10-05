using System;
using System.Collections.Generic;

namespace WorkGrid.Domain.Sync;

public sealed record SyncManifest(Guid ReplicaId, IReadOnlyDictionary<SyncObjectKey, SyncObjectSummary> Objects);
