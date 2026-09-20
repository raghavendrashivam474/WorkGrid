using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class LocalLifecyclePersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public LocalLifecyclePersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var initContext = CreateContext();
        initContext.Database.EnsureCreated();
    }

    private WorkGridDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new WorkGridDbContext(options);
    }

    [Fact]
    public async Task FullWorkflow_SurvivesSimulatedAppRestart_AndMaintainsDataIntegrity()
    {
        var employeeId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        // ── Session 1: Create Employee and Asset, then Assign ──
        using (var db1 = CreateContext())
        {
            var empRepo = new EmployeeRepository(db1);
            var astRepo = new AssetRepository(db1);
            var asmRepo = new AssignmentRepository(db1);
            var service = new AssignmentService(asmRepo, astRepo, empRepo, db1);

            await empRepo.AddAsync(new Employee(employeeId, "EMP-200", "David Brown", "david@company.local"));
            await astRepo.AddAsync(new Asset(assetId, "AST-999", "ThinkPad X1", "Laptop", "TP-8877"));

            await service.AssignAssetAsync(employeeId, assetId);
        }

        // ── Session 2 (Simulated App Restart): Verify State ──
        Guid activeAssignmentId;
        using (var db2 = CreateContext())
        {
            var astRepo = new AssetRepository(db2);
            var asmRepo = new AssignmentRepository(db2);

            var asset = await astRepo.GetByIdAsync(assetId);
            Assert.NotNull(asset);
            Assert.Equal(AssetStatus.Assigned, asset.Status);

            var activeAssignments = await asmRepo.GetByAssetIdAsync(assetId);
            Assert.Single(activeAssignments);
            Assert.Equal(AssignmentStatus.Active, activeAssignments[0].Status);
            activeAssignmentId = activeAssignments[0].Id;
        }

        // ── Session 3: Return the Asset ──
        using (var db3 = CreateContext())
        {
            var empRepo = new EmployeeRepository(db3);
            var astRepo = new AssetRepository(db3);
            var asmRepo = new AssignmentRepository(db3);
            var service = new AssignmentService(asmRepo, astRepo, empRepo, db3);

            await service.ReturnAssetAsync(activeAssignmentId);
        }

        // ── Session 4 (Second App Restart): Verify Return Persisted ──
        using (var db4 = CreateContext())
        {
            var astRepo = new AssetRepository(db4);
            var asmRepo = new AssignmentRepository(db4);

            var asset = await astRepo.GetByIdAsync(assetId);
            Assert.NotNull(asset);
            Assert.Equal(AssetStatus.Available, asset.Status);

            var assignment = await asmRepo.GetByIdAsync(activeAssignmentId);
            Assert.NotNull(assignment);
            Assert.Equal(AssignmentStatus.Returned, assignment.Status);
            Assert.NotNull(assignment.ReturnedAt);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
