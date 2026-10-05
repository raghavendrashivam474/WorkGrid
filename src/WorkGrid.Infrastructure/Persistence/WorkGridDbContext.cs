using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Sync;

namespace WorkGrid.Infrastructure.Persistence;

public sealed class WorkGridDbContext : DbContext
{
    // ── Existing DbSets (unchanged) ──────────────────────────────
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<User> Users => Set<User>();

    // ── S5.4 Sync metadata DbSets (additive) ─────────────────────
    public DbSet<SyncDeviceRecord> SyncDevices => Set<SyncDeviceRecord>();
    public DbSet<SyncChangeRecord> SyncChangeRecords => Set<SyncChangeRecord>();
    public DbSet<SyncCheckpointRecord> SyncCheckpoints => Set<SyncCheckpointRecord>();

    public WorkGridDbContext(DbContextOptions<WorkGridDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkGridDbContext).Assembly);
    }

    // ── S5.4 Change Capture via SaveChanges Interception ─────────
    // Option B from the implementation brief: intercept SaveChanges
    // to atomically record sync metadata alongside domain mutations.
    // No repository or entity modifications required.

    private static readonly HashSet<Type> SyncableEntityTypes = new()
    {
        typeof(Employee),
        typeof(Asset),
        typeof(Assignment)
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await CaptureSyncChangesAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        CaptureSyncChanges();
        return base.SaveChanges();
    }

    // ── Async capture path ───────────────────────────────────────
    private async Task CaptureSyncChangesAsync(CancellationToken ct)
    {
        var relevantEntries = GetRelevantEntries();
        if (relevantEntries.Count == 0) return;

        var deviceId = await EnsureDeviceIdentityAsync(ct);
        var nextSeq = await GetNextSequenceAsync(deviceId, ct);

        AttachChangeRecords(relevantEntries, deviceId, nextSeq);
    }

    // ── Sync capture path ────────────────────────────────────────
    private void CaptureSyncChanges()
    {
        var relevantEntries = GetRelevantEntries();
        if (relevantEntries.Count == 0) return;

        var deviceId = EnsureDeviceIdentity();
        var nextSeq = GetNextSequence(deviceId);

        AttachChangeRecords(relevantEntries, deviceId, nextSeq);
    }

    // ── Shared logic ─────────────────────────────────────────────
    private List<EntityEntry> GetRelevantEntries()
    {
        return ChangeTracker.Entries()
            .Where(e => SyncableEntityTypes.Contains(e.Entity.GetType()))
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
    }

    private void AttachChangeRecords(List<EntityEntry> entries, Guid deviceId, long startingSequence)
    {
        var now = DateTimeOffset.UtcNow;
        var seq = startingSequence;

        foreach (var entry in entries)
        {
            seq++;
            var entityType = entry.Entity.GetType();

            var syncObjectType = entityType.Name switch
            {
                nameof(Employee) => SyncObjectType.Employee,
                nameof(Asset) => SyncObjectType.Asset,
                nameof(Assignment) => SyncObjectType.Assignment,
                _ => throw new InvalidOperationException($"Unknown syncable type: {entityType.Name}")
            };

            var operation = entry.State switch
            {
                EntityState.Added => SyncOperation.Create,
                EntityState.Modified => SyncOperation.Update,
                EntityState.Deleted => SyncOperation.Delete,
                _ => throw new InvalidOperationException($"Unexpected state: {entry.State}")
            };

            var objectId = (Guid)entry.Property("Id").CurrentValue!;

            // Tombstone: null payload for deletes.
            // Create/Update: serialize current property values.
            string? payload = entry.State == EntityState.Deleted
                ? null
                : JsonSerializer.Serialize(entry.CurrentValues.ToObject());

            var hash = ComputePayloadHash(payload ?? string.Empty);

            SyncChangeRecords.Add(new SyncChangeRecord
            {
                ChangeId = Guid.NewGuid(),
                ObjectType = (int)syncObjectType,
                ObjectId = objectId,
                Operation = (int)operation,
                OriginatingDeviceId = deviceId,
                SequenceNumber = seq,
                Payload = payload,
                PayloadHash = hash,
                CreatedAt = now
            });
        }
    }

    // ── Device identity ──────────────────────────────────────────
    private async Task<Guid> EnsureDeviceIdentityAsync(CancellationToken ct)
    {
        var device = await SyncDevices.FirstOrDefaultAsync(ct);
        if (device is not null) return device.DeviceId;

        device = new SyncDeviceRecord { DeviceId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        SyncDevices.Add(device);
        return device.DeviceId;
    }

    private Guid EnsureDeviceIdentity()
    {
        var device = SyncDevices.FirstOrDefault();
        if (device is not null) return device.DeviceId;

        device = new SyncDeviceRecord { DeviceId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        SyncDevices.Add(device);
        return device.DeviceId;
    }

    // ── Sequence generation ──────────────────────────────────────
    private async Task<long> GetNextSequenceAsync(Guid deviceId, CancellationToken ct)
    {
        return await SyncChangeRecords
            .Where(s => s.OriginatingDeviceId == deviceId)
            .Select(s => (long?)s.SequenceNumber)
            .MaxAsync(ct) ?? 0L;
    }

    private long GetNextSequence(Guid deviceId)
    {
        return SyncChangeRecords
            .Where(s => s.OriginatingDeviceId == deviceId)
            .Select(s => (long?)s.SequenceNumber)
            .Max() ?? 0L;
    }

    // ── Hash (matches SyncChange domain algorithm) ───────────────
    private static string ComputePayloadHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
