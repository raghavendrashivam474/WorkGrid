using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Infrastructure.Remote.Sync;

public interface ISyncTransport
{
    Task<SyncSessionResponse?> StartSessionAsync(Guid deviceId, CancellationToken ct = default);
    Task<bool> PushManifestAsync(Guid deviceId, SyncManifest manifest, CancellationToken ct = default);
    Task<SyncManifest?> PullManifestAsync(Guid targetDeviceId, CancellationToken ct = default);
    Task<bool> PushEnvelopeAsync(SyncEnvelope envelope, CancellationToken ct = default);
    Task<IReadOnlyList<SyncEnvelope>> PollPendingEnvelopesAsync(Guid deviceId, CancellationToken ct = default);
    Task<bool> AcknowledgeEnvelopeAsync(Guid deviceId, Guid envelopeId, CancellationToken ct = default);
}
