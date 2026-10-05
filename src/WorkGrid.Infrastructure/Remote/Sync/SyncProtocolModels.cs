using System;
using System.Collections.Generic;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Infrastructure.Remote.Sync;

public sealed record SyncSessionRequest(Guid DeviceId);
public sealed record SyncSessionResponse(Guid SessionId, Guid DeviceId, DateTimeOffset ExpiresAt);

public sealed record SyncManifestEnvelope(Guid SenderDeviceId, SyncManifest Manifest);

public sealed record SyncEnvelope(
    Guid EnvelopeId,
    Guid SenderDeviceId,
    Guid RecipientDeviceId,
    IReadOnlyList<SyncChange> Changes,
    DateTimeOffset CreatedAt);

public sealed record SyncAckRequest(Guid EnvelopeId, Guid DeviceId);
