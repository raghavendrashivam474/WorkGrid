using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Sync;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Sync;

public sealed class SyncEngineIntegrationTests : IDisposable
{
    private readonly string _dbPathA;
    private readonly string _dbPathB;
    private readonly DbContextOptions<WorkGridDbContext> _optionsA;
    private readonly DbContextOptions<WorkGridDbContext> _optionsB;

    public SyncEngineIntegrationTests()
    {
        _dbPathA = Path.Combine(Path.GetTempPath(), $"workgrid_replica_A_{Guid.NewGuid():N}.db");
        _dbPathB = Path.Combine(Path.GetTempPath(), $"workgrid_replica_B_{Guid.NewGuid():N}.db");

        _optionsA = new DbContextOptionsBuilder<WorkGridDbContext>().UseSqlite($"Data Source={_dbPathA}").Options;
        _optionsB = new DbContextOptionsBuilder<WorkGridDbContext>().UseSqlite($"Data Source={_dbPathB}").Options;

        using var ctxA = new WorkGridDbContext(_optionsA);
        ctxA.Database.Migrate();

        using var ctxB = new WorkGridDbContext(_optionsB);
        ctxB.Database.Migrate();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPathA)) try { File.Delete(_dbPathA); } catch { }
        if (File.Exists(_dbPathB)) try { File.Delete(_dbPathB); } catch { }
    }

    [Fact]
    public async Task CompleteReplication_FromReplicaA_ToReplicaB_ConvergesSuccessfully()
    {
        var empId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assignId = Guid.NewGuid();
        Guid deviceIdA;

        // 1. Replica A performs mutations
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var empRepo = new EmployeeRepository(ctxA);
            var assetRepo = new AssetRepository(ctxA);

            await empRepo.AddAsync(new Employee(empId, "EMP-001", "Raghav Sharma", "raghav@workgrid.io", "Core"));
            await assetRepo.AddAsync(new Asset(assetId, "LAP-001", "ThinkPad P1"));

            var assign = new Assignment(assignId, empId, assetId, DateTimeOffset.UtcNow);
            ctxA.Assignments.Add(assign);
            await ctxA.SaveChangesAsync();

            var dev = await ctxA.SyncDevices.FirstAsync();
            deviceIdA = dev.DeviceId;
        }

        // 2. Generate manifests
        SyncManifest manifestA;
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var engineA = new SyncEngine(ctxA);
            manifestA = await engineA.GenerateLocalManifestAsync();
        }

        SyncManifest manifestB;
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            manifestB = await engineB.GenerateLocalManifestAsync();
        }

        // 3. Reconcile
        var reconciliationResults = SyncReconciler.Reconcile(manifestB, manifestA).ToList();
        var missingOnB = reconciliationResults
            .Where(r => r.Status is ReconciliationStatus.MissingLocally or ReconciliationStatus.NewerRemotely)
            .Select(r => r.Key)
            .ToList();

        Assert.Equal(3, missingOnB.Count);

        // 4. Select changes on A
        IReadOnlyList<SyncChange> changesToSend;
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var engineA = new SyncEngine(ctxA);
            changesToSend = await engineA.SelectLocalChangesAsync(missingOnB, manifestB);
        }

        Assert.Equal(3, changesToSend.Count);

        // 5. Apply changes on B
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            await engineB.ApplyRemoteChangesAsync(deviceIdA, changesToSend);
        }

        // 6. Verify Replica B state matches Replica A
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var empB = await ctxB.Employees.FindAsync(empId);
            Assert.NotNull(empB);
            Assert.Equal("Raghav Sharma", empB.Name);
            Assert.Equal("EMP-001", empB.EmployeeCode);

            var assetB = await ctxB.Assets.FindAsync(assetId);
            Assert.NotNull(assetB);
            Assert.Equal("ThinkPad P1", assetB.Name);

            var assignB = await ctxB.Assignments.FindAsync(assignId);
            Assert.NotNull(assignB);
            Assert.Equal(empId, assignB.EmployeeId);
            Assert.Equal(assetId, assignB.AssetId);

            // Verify checkpoint updated
            var checkpoint = await ctxB.SyncCheckpoints.FindAsync(deviceIdA);
            Assert.NotNull(checkpoint);
            Assert.Equal(3L, checkpoint.LastAppliedSequenceNumber);
        }
    }

    [Fact]
    public async Task Idempotency_ApplyingSameChangesTwice_DoesNotCorruptOrDuplicateState()
    {
        var empId = Guid.NewGuid();
        Guid deviceIdA;
        IReadOnlyList<SyncChange> changes;

        // Replica A creates Employee
        using (var ctxA = new WorkGridDbContext(_optionsA))
        {
            var empRepo = new EmployeeRepository(ctxA);
            await empRepo.AddAsync(new Employee(empId, "EMP-IDEM", "Idem User", "idem@workgrid.io"));
            var dev = await ctxA.SyncDevices.FirstAsync();
            deviceIdA = dev.DeviceId;

            var engineA = new SyncEngine(ctxA);
            var manifestA = await engineA.GenerateLocalManifestAsync();
            changes = await engineA.SelectLocalChangesAsync(manifestA.Objects.Keys, new SyncManifest(Guid.NewGuid(), new System.Collections.Generic.Dictionary<SyncObjectKey, SyncObjectSummary>()));
        }

        // Apply 1st time to B
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            await engineB.ApplyRemoteChangesAsync(deviceIdA, changes);
        }

        // Apply 2nd time to B (simulated network replay/retry)
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var engineB = new SyncEngine(ctxB);
            await engineB.ApplyRemoteChangesAsync(deviceIdA, changes);
        }

        // Verify exactly one employee exists on B
        using (var ctxB = new WorkGridDbContext(_optionsB))
        {
            var employees = await ctxB.Employees.ToListAsync();
            Assert.Single(employees);
            Assert.Equal(empId, employees[0].Id);
        }
    }

    [Fact]
    public async Task DependencyValidation_MissingEmployeeOrAsset_ThrowsDomainValidationException()
    {
        var unresolvableAssignmentChange = new SyncChange(
            Guid.NewGuid(),
            SyncObjectType.Assignment,
            Guid.NewGuid(),
            SyncOperation.Create,
            Guid.NewGuid(),
            1,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(), // Non-existent employee
                AssetId = Guid.NewGuid(),    // Non-existent asset
                AssignedAt = DateTimeOffset.UtcNow,
                Status = AssignmentStatus.Active
            }),
            DateTimeOffset.UtcNow);

        using var ctxB = new WorkGridDbContext(_optionsB);
        var engineB = new SyncEngine(ctxB);

        await Assert.ThrowsAsync<DomainValidationException>(async () =>
        {
            await engineB.ApplyRemoteChangesAsync(Guid.NewGuid(), new[] { unresolvableAssignmentChange });
        });
    }
}
