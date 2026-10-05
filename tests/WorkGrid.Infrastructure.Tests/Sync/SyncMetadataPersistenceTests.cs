using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Sync;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Sync;

public sealed class SyncMetadataPersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<WorkGridDbContext> _options;

    public SyncMetadataPersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"workgrid_sync_test_{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        // Initialize schema using real migrations
        using var context = new WorkGridDbContext(_options);
        context.Database.Migrate();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public async Task DeviceIdentity_IsCreatedOnFirstMutation_AndPersistsAcrossContextRecreation()
    {
        Guid initialDeviceId;

        // Act 1: Trigger first mutation in Context 1
        using (var context1 = new WorkGridDbContext(_options))
        {
            var repo = new EmployeeRepository(context1);
            var employee = new Employee(Guid.NewGuid(), "EMP-001", "Alice Smith", "alice@example.com", "Eng");
            await repo.AddAsync(employee);

            var deviceRecord = await context1.SyncDevices.SingleAsync();
            initialDeviceId = deviceRecord.DeviceId;
            Assert.NotEqual(Guid.Empty, initialDeviceId);
        }

        // Act 2: Perform second mutation in brand new Context 2 (simulating restart)
        using (var context2 = new WorkGridDbContext(_options))
        {
            var repo = new EmployeeRepository(context2);
            var employee2 = new Employee(Guid.NewGuid(), "EMP-002", "Bob Jones", "bob@example.com", "Design");
            await repo.AddAsync(employee2);

            var deviceRecords = await context2.SyncDevices.ToListAsync();
            Assert.Single(deviceRecords);
            Assert.Equal(initialDeviceId, deviceRecords[0].DeviceId);

            var changes = await context2.SyncChangeRecords.OrderBy(c => c.SequenceNumber).ToListAsync();
            Assert.Equal(2, changes.Count);
            Assert.All(changes, c => Assert.Equal(initialDeviceId, c.OriginatingDeviceId));
        }
    }

    [Fact]
    public async Task SequenceNumbers_AreStrictlyMonotonic_AcrossRestartsAndMultipleEntityTypes()
    {
        var empId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        using (var context = new WorkGridDbContext(_options))
        {
            var empRepo = new EmployeeRepository(context);
            var assetRepo = new AssetRepository(context);

            // Sequence 1
            await empRepo.AddAsync(new Employee(empId, "EMP-100", "Charlie", "charlie@test.com"));
            // Sequence 2
            await assetRepo.AddAsync(new Asset(assetId, "LAP-001", "MacBook Pro"));
        }

        // Simulate app restart / new context
        using (var context = new WorkGridDbContext(_options))
        {
            var empRepo = new EmployeeRepository(context);
            var emp = await empRepo.GetByIdAsync(empId);
            Assert.NotNull(emp);

            // Sequence 3
            emp.UpdateDetails("Charlie Brown", "charlie.b@test.com", "Engineering");
            await empRepo.UpdateAsync(emp);

            // Sequence 4 (Assignment)
            var assign = new Assignment(Guid.NewGuid(), empId, assetId, DateTimeOffset.UtcNow);
            context.Assignments.Add(assign);
            await context.SaveChangesAsync();
        }

        // Verify full sequence integrity
        using (var context = new WorkGridDbContext(_options))
        {
            var changes = await context.SyncChangeRecords.OrderBy(c => c.SequenceNumber).ToListAsync();
            Assert.Equal(4, changes.Count);
            Assert.Equal(1L, changes[0].SequenceNumber);
            Assert.Equal(2L, changes[1].SequenceNumber);
            Assert.Equal(3L, changes[2].SequenceNumber);
            Assert.Equal(4L, changes[3].SequenceNumber);

            Assert.Equal((int)SyncObjectType.Employee, changes[0].ObjectType);
            Assert.Equal((int)SyncOperation.Create, changes[0].Operation);

            Assert.Equal((int)SyncObjectType.Asset, changes[1].ObjectType);
            Assert.Equal((int)SyncOperation.Create, changes[1].Operation);

            Assert.Equal((int)SyncObjectType.Employee, changes[2].ObjectType);
            Assert.Equal((int)SyncOperation.Update, changes[2].Operation);

            Assert.Equal((int)SyncObjectType.Assignment, changes[3].ObjectType);
            Assert.Equal((int)SyncOperation.Create, changes[3].Operation);
        }
    }

    [Fact]
    public async Task Delete_ProducesTombstoneRecord_WithNullPayloadAndCorrectTargetId()
    {
        var assetId = Guid.NewGuid();

        using (var context = new WorkGridDbContext(_options))
        {
            var repo = new AssetRepository(context);
            var asset = new Asset(assetId, "MON-999", "Dell 4K Display");
            await repo.AddAsync(asset);
        }

        // Delete asset in separate transaction
        using (var context = new WorkGridDbContext(_options))
        {
            var repo = new AssetRepository(context);
            var asset = await repo.GetByIdAsync(assetId);
            Assert.NotNull(asset);

            await repo.DeleteAsync(asset);
        }

        // Verify tombstone record
        using (var context = new WorkGridDbContext(_options))
        {
            var changes = await context.SyncChangeRecords
                .Where(c => c.ObjectId == assetId)
                .OrderBy(c => c.SequenceNumber)
                .ToListAsync();

            Assert.Equal(2, changes.Count);

            var deleteChange = changes[1];
            Assert.Equal((int)SyncOperation.Delete, deleteChange.Operation);
            Assert.Equal((int)SyncObjectType.Asset, deleteChange.ObjectType);
            Assert.Null(deleteChange.Payload);
            Assert.False(string.IsNullOrEmpty(deleteChange.PayloadHash)); // hash of empty string

            // Confirm asset is actually gone from active table
            var assetInDb = await context.Assets.FindAsync(assetId);
            Assert.Null(assetInDb);
        }
    }

    [Fact]
    public async Task Atomicity_FailedTransaction_DoesNotPersistSyncMetadata()
    {
        using (var context = new WorkGridDbContext(_options))
        {
            var existingEmp = new Employee(Guid.NewGuid(), "EMP-DUP", "First", "first@test.com");
            context.Employees.Add(existingEmp);
            await context.SaveChangesAsync();
        }

        // Attempt to add another employee with duplicate code (will violate unique index constraint)
        using (var context = new WorkGridDbContext(_options))
        {
            var duplicateEmp = new Employee(Guid.NewGuid(), "EMP-DUP", "Second", "second@test.com");
            context.Employees.Add(duplicateEmp);

            // DbUpdateException expected due to SQLite unique constraint on EmployeeCode
            await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
            {
                await context.SaveChangesAsync();
            });
        }

        // Verify that NO second sync change was committed
        using (var context = new WorkGridDbContext(_options))
        {
            var totalChanges = await context.SyncChangeRecords.CountAsync();
            Assert.Equal(1, totalChanges);

            var totalEmployees = await context.Employees.CountAsync();
            Assert.Equal(1, totalEmployees);
        }
    }

    [Fact]
    public async Task SyncCheckpoint_PersistsAndUpdatesSuccessfully()
    {
        var remoteReplicaId = Guid.NewGuid();

        using (var context = new WorkGridDbContext(_options))
        {
            var checkpoint = new SyncCheckpointRecord
            {
                RemoteReplicaId = remoteReplicaId,
                LastAppliedSequenceNumber = 15,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.SyncCheckpoints.Add(checkpoint);
            await context.SaveChangesAsync();
        }

        // Read and update checkpoint in new context
        using (var context = new WorkGridDbContext(_options))
        {
            var cp = await context.SyncCheckpoints.FindAsync(remoteReplicaId);
            Assert.NotNull(cp);
            Assert.Equal(15L, cp.LastAppliedSequenceNumber);

            cp.LastAppliedSequenceNumber = 20;
            cp.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        // Verify updated checkpoint
        using (var context = new WorkGridDbContext(_options))
        {
            var cp = await context.SyncCheckpoints.FindAsync(remoteReplicaId);
            Assert.NotNull(cp);
            Assert.Equal(20L, cp.LastAppliedSequenceNumber);
        }
    }
}
