using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Remote;
using WorkGrid.Infrastructure.Remote.Sync;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using WorkGrid.Infrastructure.Sync;
using Xunit;

namespace WorkGrid.Api.Tests;

public sealed class SyncRelayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITokenService _tokenService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _dbPathA;
    private readonly string _dbPathB;
    private readonly DbContextOptions<WorkGridDbContext> _optionsA;
    private readonly DbContextOptions<WorkGridDbContext> _optionsB;

    public SyncRelayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _tokenService = new JwtTokenService();

        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new SyncObjectKeyJsonConverter());

        _dbPathA = Path.Combine(Path.GetTempPath(), $"workgrid_relay_A_{Guid.NewGuid():N}.db");
        _dbPathB = Path.Combine(Path.GetTempPath(), $"workgrid_relay_B_{Guid.NewGuid():N}.db");

        _optionsA = new DbContextOptionsBuilder<WorkGridDbContext>().UseSqlite($"Data Source={_dbPathA}").Options;
        _optionsB = new DbContextOptionsBuilder<WorkGridDbContext>().UseSqlite($"Data Source={_dbPathB}").Options;

        using var ctxA = new WorkGridDbContext(_optionsA);
        ctxA.Database.Migrate();

        using var ctxB = new WorkGridDbContext(_optionsB);
        ctxB.Database.Migrate();
    }

    public void Dispose()
    {
        _client.Dispose();
        if (File.Exists(_dbPathA)) try { File.Delete(_dbPathA); } catch { }
        if (File.Exists(_dbPathB)) try { File.Delete(_dbPathB); } catch { }
    }

    [Fact]
    public async Task SessionEndpoint_RequiresAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/sync/session", new SyncSessionRequest(Guid.NewGuid()), _jsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TwoDevice_RelayReplication_ManifestExchange_AndChangeDelivery_Converges()
    {
        // 1. Authenticate both devices
        var user = new User(Guid.NewGuid(), "sync_admin", "hash", "Sync Admin", UserRole.Admin);
        var token = _tokenService.GenerateToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Device A setup & local mutation
        var empId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        Guid deviceIdA;

        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var empRepo = new EmployeeRepository(ctxA);
            var assetRepo = new AssetRepository(ctxA);

            await empRepo.AddAsync(new Employee(empId, "EMP-777", "Relay Master", "master@workgrid.io", "SyncOps"));
            await assetRepo.AddAsync(new Asset(assetId, "RELAY-01", "Relay Edge Node"));

            var dev = await ctxA.SyncDevices.FirstAsync();
            deviceIdA = dev.DeviceId;
        }

        // 3. Device A uploads Manifest to Relay
        SyncManifest manifestA;
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var engineA = new SyncEngine(ctxA);
            manifestA = await engineA.GenerateLocalManifestAsync();
        }

        var postManifestResp = await _client.PostAsJsonAsync("/api/sync/manifest", new SyncManifestEnvelope(deviceIdA, manifestA), _jsonOptions);
        Assert.True(postManifestResp.IsSuccessStatusCode);

        // 4. Device B gets Device A's manifest from Relay
        var pullManifestResp = await _client.GetAsync($"/api/sync/manifest/{deviceIdA}");
        Assert.True(pullManifestResp.IsSuccessStatusCode);
        var remoteManifestFromRelay = await pullManifestResp.Content.ReadFromJsonAsync<SyncManifest>(_jsonOptions);
        Assert.NotNull(remoteManifestFromRelay);

        // 5. Device B reconciles
        SyncManifest manifestB;
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            manifestB = await engineB.GenerateLocalManifestAsync();
        }

        var reconciliation = SyncReconciler.Reconcile(manifestB, remoteManifestFromRelay).ToList();
        var missingOnB = reconciliation
            .Where(r => r.Status is ReconciliationStatus.MissingLocally or ReconciliationStatus.NewerRemotely)
            .Select(r => r.Key)
            .ToList();

        Assert.Equal(2, missingOnB.Count);

        // 6. Device A selects changes and pushes Envelope to Relay
        IReadOnlyList<SyncChange> changesToSend;
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var engineA = new SyncEngine(ctxA);
            changesToSend = await engineA.SelectLocalChangesAsync(missingOnB, manifestB);
        }

        var deviceIdB = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new SyncEnvelope(envelopeId, deviceIdA, deviceIdB, changesToSend, DateTimeOffset.UtcNow);

        var pushEnvResp = await _client.PostAsJsonAsync("/api/sync/envelope", envelope, _jsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, pushEnvResp.StatusCode);

        // 7. Device B polls pending envelopes from Relay
        var pollResp = await _client.GetAsync($"/api/sync/pending/{deviceIdB}");
        Assert.True(pollResp.IsSuccessStatusCode);
        var pendingEnvelopes = await pollResp.Content.ReadFromJsonAsync<List<SyncEnvelope>>(_jsonOptions);
        Assert.NotNull(pendingEnvelopes);
        Assert.Single(pendingEnvelopes);

        var receivedEnvelope = pendingEnvelopes[0];
        Assert.Equal(envelopeId, receivedEnvelope.EnvelopeId);

        // 8. Device B applies changes to its local database
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            await engineB.ApplyRemoteChangesAsync(deviceIdA, receivedEnvelope.Changes);
        }

        // 9. Device B acknowledges receipt to Relay
        var ackResp = await _client.PostAsJsonAsync("/api/sync/ack", new SyncAckRequest(envelopeId, deviceIdB), _jsonOptions);
        Assert.True(ackResp.IsSuccessStatusCode);

        // 10. Verify Relay queue is now empty for Device B
        var pollAfterAck = await _client.GetAsync($"/api/sync/pending/{deviceIdB}");
        var remainingPending = await pollAfterAck.Content.ReadFromJsonAsync<List<SyncEnvelope>>(_jsonOptions);
        Assert.NotNull(remainingPending);
        Assert.Empty(remainingPending);

        // 11. Verify Device B local database converged with Device A
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var empB = await ctxB.Employees.FindAsync(empId);
            Assert.NotNull(empB);
            Assert.Equal("Relay Master", empB.Name);
            Assert.Equal("EMP-777", empB.EmployeeCode);

            var assetB = await ctxB.Assets.FindAsync(assetId);
            Assert.NotNull(assetB);
            Assert.Equal("RELAY-01", assetB.AssetTag);

            var checkpoint = await ctxB.SyncCheckpoints.FindAsync(deviceIdA);
            Assert.NotNull(checkpoint);
            Assert.Equal(2L, checkpoint.LastAppliedSequenceNumber);
        }
    }
}
