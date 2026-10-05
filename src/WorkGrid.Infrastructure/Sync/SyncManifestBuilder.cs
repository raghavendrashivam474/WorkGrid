using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Sync;

public sealed class SyncManifestBuilder
{
    private readonly WorkGridDbContext _context;

    public SyncManifestBuilder(WorkGridDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SyncManifest> BuildAsync(CancellationToken ct = default)
    {
        // 1. Get or create stable local device/replica ID
        var deviceRecord = await _context.SyncDevices.FirstOrDefaultAsync(ct);
        var replicaId = deviceRecord?.DeviceId ?? Guid.NewGuid();

        var objects = new Dictionary<SyncObjectKey, SyncObjectSummary>();

        // 2. Fetch latest change records per (ObjectType, ObjectId)
        var latestChanges = await _context.SyncChangeRecords
            .GroupBy(c => new { c.ObjectType, c.ObjectId })
            .Select(g => g.OrderByDescending(c => c.SequenceNumber).First())
            .ToListAsync(ct);

        var latestChangesLookup = latestChangesToLookup(latestChanges);

        // 3. Process active Employees
        var employees = await _context.Employees.AsNoTracking().ToListAsync(ct);
        foreach (var emp in employees)
        {
            var key = new SyncObjectKey(SyncObjectType.Employee, emp.Id);
            var version = GetVersion(key, replicaId, latestChangesLookup);
            var stateHash = ComputeStateHash(emp);
            objects[key] = new SyncObjectSummary(key, version, stateHash);
        }

        // 4. Process active Assets
        var assets = await _context.Assets.AsNoTracking().ToListAsync(ct);
        foreach (var asset in assets)
        {
            var key = new SyncObjectKey(SyncObjectType.Asset, asset.Id);
            var version = GetVersion(key, replicaId, latestChangesLookup);
            var stateHash = ComputeStateHash(asset);
            objects[key] = new SyncObjectSummary(key, version, stateHash);
        }

        // 5. Process active Assignments
        var assignments = await _context.Assignments.AsNoTracking().ToListAsync(ct);
        foreach (var assign in assignments)
        {
            var key = new SyncObjectKey(SyncObjectType.Assignment, assign.Id);
            var version = GetVersion(key, replicaId, latestChangesLookup);
            var stateHash = ComputeStateHash(assign);
            objects[key] = new SyncObjectSummary(key, version, stateHash);
        }

        // 6. Process Tombstones (items where latest sync record is Delete)
        var deletedRecords = latestChanges
            .Where(c => c.Operation == (int)SyncOperation.Delete);

        foreach (var del in deletedRecords)
        {
            var key = new SyncObjectKey((SyncObjectType)del.ObjectType, del.ObjectId);
            var version = new SyncVersion(del.OriginatingDeviceId, del.SequenceNumber);
            // Represent tombstones with a specific static hash so reconciler matches correctly
            objects[key] = new SyncObjectSummary(key, version, "TOMBSTONE");
        }

        return new SyncManifest(replicaId, objects);
    }

    private static Dictionary<SyncObjectKey, SyncChangeRecord> latestChangesToLookup(List<SyncChangeRecord> list)
    {
        var lookup = new Dictionary<SyncObjectKey, SyncChangeRecord>();
        foreach (var record in list)
        {
            var key = new SyncObjectKey((SyncObjectType)record.ObjectType, record.ObjectId);
            lookup[key] = record;
        }
        return lookup;
    }

    private static SyncVersion GetVersion(
        SyncObjectKey key,
        Guid replicaId,
        Dictionary<SyncObjectKey, SyncChangeRecord> lookup)
    {
        if (lookup.TryGetValue(key, out var record))
        {
            return new SyncVersion(record.OriginatingDeviceId, record.SequenceNumber);
        }
        // Fallback version for pre-existing seed data
        return new SyncVersion(replicaId, 1L);
    }

    private static string ComputeStateHash<T>(T obj) where T : class
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
