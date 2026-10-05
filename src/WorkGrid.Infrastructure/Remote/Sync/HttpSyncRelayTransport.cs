using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Infrastructure.Remote.Sync;

public sealed class HttpSyncRelayTransport : ISyncTransport, IDisposable
{
    private readonly IRemoteClient _remoteClient;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public HttpSyncRelayTransport(IRemoteClient remoteClient)
    {
        _remoteClient = remoteClient ?? throw new ArgumentNullException(nameof(remoteClient));
        _httpClient = new HttpClient
        {
            BaseAddress = remoteClient.Endpoint.BaseUri,
            Timeout = remoteClient.Endpoint.Timeout
        };
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new SyncObjectKeyJsonConverter());
    }

    public async Task<SyncSessionResponse?> StartSessionAsync(Guid deviceId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.PostAsJsonAsync("/api/sync/session", new SyncSessionRequest(deviceId), _jsonOptions, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<SyncSessionResponse>(_jsonOptions, cancellationToken: ct);
    }

    public async Task<bool> PushManifestAsync(Guid deviceId, SyncManifest manifest, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var envelope = new SyncManifestEnvelope(deviceId, manifest);
        var response = await _httpClient.PostAsJsonAsync("/api/sync/manifest", envelope, _jsonOptions, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<SyncManifest?> PullManifestAsync(Guid targetDeviceId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"/api/sync/manifest/{targetDeviceId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<SyncManifest>(_jsonOptions, cancellationToken: ct);
    }

    public async Task<bool> PushEnvelopeAsync(SyncEnvelope envelope, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.PostAsJsonAsync("/api/sync/envelope", envelope, _jsonOptions, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<SyncEnvelope>> PollPendingEnvelopesAsync(Guid deviceId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync($"/api/sync/pending/{deviceId}", ct);
        if (!response.IsSuccessStatusCode) return Array.Empty<SyncEnvelope>();

        var envelopes = await response.Content.ReadFromJsonAsync<List<SyncEnvelope>>(_jsonOptions, cancellationToken: ct);
        return envelopes ?? (IReadOnlyList<SyncEnvelope>)Array.Empty<SyncEnvelope>();
    }

    public async Task<bool> AcknowledgeEnvelopeAsync(Guid deviceId, Guid envelopeId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var req = new SyncAckRequest(envelopeId, deviceId);
        var response = await _httpClient.PostAsJsonAsync("/api/sync/ack", req, _jsonOptions, ct);
        return response.IsSuccessStatusCode;
    }

    private void ApplyAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrWhiteSpace(_remoteClient.AuthToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _remoteClient.AuthToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
