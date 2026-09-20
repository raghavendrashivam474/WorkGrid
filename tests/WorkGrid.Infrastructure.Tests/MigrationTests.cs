using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly string _dbPath;

    public MigrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"workgrid_migration_test_{Guid.NewGuid():N}.db3");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private WorkGridDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        return new WorkGridDbContext(options);
    }

    [Fact]
    public async Task MigrateAsync_OnFreshDatabase_CreatesSchemaAndAllowsOperations()
    {
        // Arrange
        await using (var context = CreateContext())
        {
            // Act: Apply migrations
            await context.Database.MigrateAsync();

            // Assert: Can insert and read data
            var employee = new Employee(Guid.NewGuid(), "EMP-MIG01", "Migration User", "mig@workgrid.local", "Engineering");
            var asset = new Asset(Guid.NewGuid(), "AST-MIG01", "Laptop Mig", "Hardware", "SN-MIG-1", AssetStatus.Available);

            context.Employees.Add(employee);
            context.Assets.Add(asset);
            await context.SaveChangesAsync();
        }

        // Verify in fresh context instance
        await using (var verifyContext = CreateContext())
        {
            var savedEmployee = await verifyContext.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == "EMP-MIG01");
            var savedAsset = await verifyContext.Assets.FirstOrDefaultAsync(a => a.AssetTag == "AST-MIG01");

            Assert.NotNull(savedEmployee);
            Assert.Equal("Migration User", savedEmployee.Name);
            Assert.NotNull(savedAsset);
            Assert.Equal("Laptop Mig", savedAsset.Name);
        }
    }

    [Fact]
    public async Task MigrateAsync_OnExistingDatabase_PreservesDataAcrossContextLifecycle()
    {
        var empId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assignId = Guid.NewGuid();

        // 1. First run: Migrate and populate data
        await using (var context1 = CreateContext())
        {
            await context1.Database.MigrateAsync();

            var emp = new Employee(empId, "EMP-PERSIST", "Persist User", "persist@workgrid.local");
            var asset = new Asset(assetId, "AST-PERSIST", "Persist Monitor", "Display", "SN-P1", AssetStatus.Assigned);
            var assignment = new Assignment(assignId, empId, assetId, DateTimeOffset.UtcNow, null, AssignmentStatus.Active);

            context1.Employees.Add(emp);
            context1.Assets.Add(asset);
            context1.Assignments.Add(assignment);
            await context1.SaveChangesAsync();
        }

        // 2. Second run: Simulate app restart, migrate again (idempotent), verify data intact
        await using (var context2 = CreateContext())
        {
            var pendingMigrations = await context2.Database.GetPendingMigrationsAsync();
            Assert.Empty(pendingMigrations);

            await context2.Database.MigrateAsync();

            var retrievedEmp = await context2.Employees.FindAsync(empId);
            var retrievedAsset = await context2.Assets.FindAsync(assetId);
            var retrievedAssignment = await context2.Assignments.FindAsync(assignId);

            Assert.NotNull(retrievedEmp);
            Assert.Equal("EMP-PERSIST", retrievedEmp.EmployeeCode);
            Assert.NotNull(retrievedAsset);
            Assert.Equal(AssetStatus.Assigned, retrievedAsset.Status);
            Assert.NotNull(retrievedAssignment);
            Assert.Equal(AssignmentStatus.Active, retrievedAssignment.Status);
        }
    }
}
