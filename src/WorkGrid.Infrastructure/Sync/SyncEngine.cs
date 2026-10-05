using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Sync;

public sealed class SyncEngine : ISyncEngine
{
    private readonly WorkGridDbContext _context;
    private readonly SyncManifestBuilder _manifestBuilder;

    public SyncEngine(WorkGridDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _manifestBuilder = new SyncManifestBuilder(context);
    }

    public async Task<SyncManifest> GenerateLocalManifestAsync(CancellationToken ct = default)
    {
        return await _manifestBuilder.BuildAsync(ct);
    }

    public async Task<IReadOnlyList<SyncChange>> SelectLocalChangesAsync(
        IEnumerable<SyncObjectKey> keys,
        SyncManifest remoteManifest,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(remoteManifest);

        var keySet = keys.ToHashSet();
        if (keySet.Count == 0)
        {
            return Array.Empty<SyncChange>();
        }

        var allLocalRecords = await _context.SyncChangeRecords
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(ct);

        var selectedChanges = new List<SyncChange>();

        foreach (var record in allLocalRecords)
        {
            var key = new SyncObjectKey((SyncObjectType)record.ObjectType, record.ObjectId);
            if (!keySet.Contains(key))
            {
                continue;
            }

            // Check if remote already knows this version or higher
            if (remoteManifest.Objects.TryGetValue(key, out var remoteSummary))
            {
                if (remoteSummary.Version.OriginatingDeviceId == record.OriginatingDeviceId &&
                    remoteSummary.Version.SequenceNumber >= record.SequenceNumber)
                {
                    continue; // remote is up to date with this change
                }
            }

            var change = new SyncChange(
                record.ChangeId,
                (SyncObjectType)record.ObjectType,
                record.ObjectId,
                (SyncOperation)record.Operation,
                record.OriginatingDeviceId,
                record.SequenceNumber,
                record.Payload,
                record.CreatedAt);

            selectedChanges.Add(change);
        }

        return selectedChanges;
    }

    public async Task ApplyRemoteChangesAsync(
        Guid remoteReplicaId,
        IReadOnlyList<SyncChange> remoteChanges,
        CancellationToken ct = default)
    {
        if (remoteReplicaId == Guid.Empty)
            throw new ArgumentException("Remote replica ID cannot be empty.", nameof(remoteReplicaId));
        ArgumentNullException.ThrowIfNull(remoteChanges);

        if (remoteChanges.Count == 0) return;

        // Verify integrity of all incoming changes upfront
        foreach (var change in remoteChanges)
        {
            if (!change.VerifyIntegrity())
            {
                throw new InvalidOperationException($"Integrity check failed for incoming change {change.ChangeId}");
            }
        }

        // Sort incoming changes: Employees and Assets first, then Assignments, ordered by Sequence
        var orderedChanges = remoteChanges
            .OrderBy(c => c.ObjectType == SyncObjectType.Assignment ? 1 : 0)
            .ThenBy(c => c.SequenceNumber)
            .ToList();

        long maxAppliedSequence = 0;

        foreach (var change in orderedChanges)
        {
            // Idempotency check: Have we already recorded this exact change ID?
            var alreadyRecorded = await _context.SyncChangeRecords
                .AnyAsync(r => r.ChangeId == change.ChangeId, ct);

            if (alreadyRecorded)
            {
                maxAppliedSequence = Math.Max(maxAppliedSequence, change.SequenceNumber);
                continue;
            }

            switch (change.ObjectType)
            {
                case SyncObjectType.Employee:
                    await ApplyEmployeeChangeAsync(change, ct);
                    break;

                case SyncObjectType.Asset:
                    await ApplyAssetChangeAsync(change, ct);
                    break;

                case SyncObjectType.Assignment:
                    await ApplyAssignmentChangeAsync(change, ct);
                    break;

                default:
                    throw new NotSupportedException($"Sync for object type '{change.ObjectType}' is not supported.");
            }

            // Persist the remote change record locally so it won't be re-applied and is reflected in manifest
            _context.SyncChangeRecords.Add(new SyncChangeRecord
            {
                ChangeId = change.ChangeId,
                ObjectType = (int)change.ObjectType,
                ObjectId = change.ObjectId,
                Operation = (int)change.Operation,
                OriginatingDeviceId = change.OriginatingDeviceId,
                SequenceNumber = change.SequenceNumber,
                Payload = change.Payload,
                PayloadHash = change.PayloadHash,
                CreatedAt = change.CreatedAt
            });

            await _context.SaveChangesAsync(ct);
            maxAppliedSequence = Math.Max(maxAppliedSequence, change.SequenceNumber);
        }

        // Update checkpoint for this remote replica
        if (maxAppliedSequence > 0)
        {
            var checkpoint = await _context.SyncCheckpoints.FindAsync(new object[] { remoteReplicaId }, ct);
            if (checkpoint is null)
            {
                checkpoint = new SyncCheckpointRecord
                {
                    RemoteReplicaId = remoteReplicaId,
                    LastAppliedSequenceNumber = maxAppliedSequence,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _context.SyncCheckpoints.Add(checkpoint);
            }
            else
            {
                if (maxAppliedSequence > checkpoint.LastAppliedSequenceNumber)
                {
                    checkpoint.LastAppliedSequenceNumber = maxAppliedSequence;
                    checkpoint.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task ApplyEmployeeChangeAsync(SyncChange change, CancellationToken ct)
    {
        var existing = await _context.Employees.FindAsync(new object[] { change.ObjectId }, ct);

        if (change.Operation == SyncOperation.Delete)
        {
            if (existing is not null)
            {
                _context.Employees.Remove(existing);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(change.Payload))
            throw new InvalidOperationException($"Payload is missing for non-delete Employee change {change.ChangeId}");

        var dto = JsonSerializer.Deserialize<EmployeePayloadDto>(change.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize Employee payload.");

        if (existing is null)
        {
            var newEmployee = new Employee(dto.Id, dto.EmployeeCode, dto.Name, dto.Email, dto.Department);
            await _context.Employees.AddAsync(newEmployee, ct);
        }
        else
        {
            existing.UpdateDetails(dto.Name, dto.Email, dto.Department);
        }
    }

    private async Task ApplyAssetChangeAsync(SyncChange change, CancellationToken ct)
    {
        var existing = await _context.Assets.FindAsync(new object[] { change.ObjectId }, ct);

        if (change.Operation == SyncOperation.Delete)
        {
            if (existing is not null)
            {
                _context.Assets.Remove(existing);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(change.Payload))
            throw new InvalidOperationException($"Payload is missing for non-delete Asset change {change.ChangeId}");

        var dto = JsonSerializer.Deserialize<AssetPayloadDto>(change.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize Asset payload.");

        if (existing is null)
        {
            var newAsset = new Asset(dto.Id, dto.AssetTag, dto.Name, dto.AssetType, dto.SerialNumber, dto.Status);
            await _context.Assets.AddAsync(newAsset, ct);
        }
        else
        {
            existing.UpdateDetails(dto.Name, dto.AssetType, dto.SerialNumber);
            // Reconcile status state transitions
            if (dto.Status == AssetStatus.Assigned && existing.Status == AssetStatus.Available)
                existing.MarkAssigned();
            else if (dto.Status == AssetStatus.Available && (existing.Status == AssetStatus.Assigned || existing.Status == AssetStatus.Maintenance))
                existing.MarkAvailable();
            else if (dto.Status == AssetStatus.Maintenance && existing.Status != AssetStatus.Maintenance && existing.Status != AssetStatus.Retired)
                existing.MarkMaintenance();
            else if (dto.Status == AssetStatus.Retired && existing.Status != AssetStatus.Retired && existing.Status != AssetStatus.Assigned)
                existing.Retire();
        }
    }

    private async Task ApplyAssignmentChangeAsync(SyncChange change, CancellationToken ct)
    {
        var existing = await _context.Assignments.FindAsync(new object[] { change.ObjectId }, ct);

        if (change.Operation == SyncOperation.Delete)
        {
            if (existing is not null)
            {
                _context.Assignments.Remove(existing);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(change.Payload))
            throw new InvalidOperationException($"Payload is missing for non-delete Assignment change {change.ChangeId}");

        var dto = JsonSerializer.Deserialize<AssignmentPayloadDto>(change.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize Assignment payload.");

        // Dependency validation: Employee and Asset must exist locally
        var empExists = await _context.Employees.AnyAsync(e => e.Id == dto.EmployeeId, ct);
        if (!empExists)
        {
            throw new DomainValidationException($"Cannot apply assignment '{dto.Id}': Referenced employee '{dto.EmployeeId}' does not exist locally.");
        }

        var assetExists = await _context.Assets.AnyAsync(a => a.Id == dto.AssetId, ct);
        if (!assetExists)
        {
            throw new DomainValidationException($"Cannot apply assignment '{dto.Id}': Referenced asset '{dto.AssetId}' does not exist locally.");
        }

        if (existing is null)
        {
            var newAssignment = new Assignment(dto.Id, dto.EmployeeId, dto.AssetId, dto.AssignedAt, dto.ReturnedAt, dto.Status);
            await _context.Assignments.AddAsync(newAssignment, ct);
        }
        else
        {
            if (dto.Status == AssignmentStatus.Returned && existing.Status != AssignmentStatus.Returned)
            {
                existing.CompleteReturn(dto.ReturnedAt ?? DateTimeOffset.UtcNow);
            }
        }
    }

    // DTO records for clean payload deserialization
    private sealed record EmployeePayloadDto(Guid Id, string EmployeeCode, string Name, string Email, string? Department);
    private sealed record AssetPayloadDto(Guid Id, string AssetTag, string Name, string? AssetType, string? SerialNumber, AssetStatus Status);
    private sealed record AssignmentPayloadDto(Guid Id, Guid EmployeeId, Guid AssetId, DateTimeOffset AssignedAt, DateTimeOffset? ReturnedAt, AssignmentStatus Status);
}
