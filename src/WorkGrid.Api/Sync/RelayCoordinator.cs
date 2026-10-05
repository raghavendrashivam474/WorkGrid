using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Remote.Sync;

namespace WorkGrid.Api.Sync;

public sealed class RelayCoordinator
{
    // Stores latest manifests uploaded by active devices
    private readonly ConcurrentDictionary<Guid, SyncManifest> _manifests = new();

    // Queues of pending envelopes per recipient device
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<SyncEnvelope>> _pendingEnvelopes = new();

    // Deduplication tracker for processed envelope IDs
    private readonly ConcurrentDictionary<Guid, byte> _processedEnvelopeIds = new();

    public void StoreManifest(Guid deviceId, SyncManifest manifest)
    {
        _manifests[deviceId] = manifest;
    }

    public SyncManifest? GetManifest(Guid deviceId)
    {
        _manifests.TryGetValue(deviceId, out var manifest);
        return manifest;
    }

    public bool EnqueueChanges(SyncEnvelope envelope)
    {
        if (_processedEnvelopeIds.ContainsKey(envelope.EnvelopeId))
        {
            return false; // Idempotent ignore
        }

        _processedEnvelopeIds.TryAdd(envelope.EnvelopeId, 0);

        var queue = _pendingEnvelopes.GetOrAdd(envelope.RecipientDeviceId, _ => new ConcurrentQueue<SyncEnvelope>());
        queue.Enqueue(envelope);
        return true;
    }

    public IReadOnlyList<SyncEnvelope> PollPending(Guid recipientDeviceId)
    {
        if (_pendingEnvelopes.TryGetValue(recipientDeviceId, out var queue))
        {
            return queue.ToArray();
        }
        return Array.Empty<SyncEnvelope>();
    }

    public void Acknowledge(Guid recipientDeviceId, Guid envelopeId)
    {
        if (_pendingEnvelopes.TryGetValue(recipientDeviceId, out var queue))
        {
            // Re-queue remaining unacknowledged items
            var remaining = queue.Where(e => e.EnvelopeId != envelopeId).ToList();
            _pendingEnvelopes[recipientDeviceId] = new ConcurrentQueue<SyncEnvelope>(remaining);
        }
    }
}
