using System;

namespace WorkGrid.Domain.Sync;

public sealed record SyncObjectKey(SyncObjectType Type, Guid Id);
