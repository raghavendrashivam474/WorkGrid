using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class OfflinePersistenceValidationTests : IDisposable
{
    private readonly string _dbFilePath;
    private readonly DbContextOptions<WorkGridDbContext> _dbOptions;

    public OfflinePersistenceValidationTests()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"workgrid_offline_test_{Guid.NewGuid():N}.db3");

        _dbOptions = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite($"Data Source={_dbFilePath}")
            .Options;

        // Fresh installation: Run migrations to generate local schema on disk
        using var initContext = new WorkGridDbContext(_dbOptions);
        initContext.Database.Migrate();
    }

    private WorkGridDbContext CreateContext() => new(_dbOptions);

    [Fact]
    public async Task CompleteOfflineWorkflow_SurvivesMultipleRestarts_OnPhysicalDisk()
    {
        var passwordHasher = new PasswordHasher();
        Guid adminUserId;
        var employeeId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        Guid assignmentId;

        // ── PHASE 1: Fresh Installation & Admin Registration (Offline) ──
        using (var db1 = CreateContext())
        {
            var userRepo = new UserRepository(db1);
            var session = new SessionService();
            var authService = new AuthenticationService(userRepo, passwordHasher, session);

            var regResult = await authService.RegisterInitialAdminAsync("offline_admin", "SecurePassword123!", "Offline Administrator");
            Assert.True(regResult.Success);
            Assert.NotNull(regResult.User);
            adminUserId = regResult.User.Id;
        }

        // ── PHASE 2 (Restart 1): Admin Login & Data Creation ──
        using (var db2 = CreateContext())
        {
            var userRepo = new UserRepository(db2);
            var session = new SessionService();
            var authService = new AuthenticationService(userRepo, passwordHasher, session);

            var loginResult = await authService.LoginAsync("offline_admin", "SecurePassword123!");
            Assert.True(loginResult.Success);
            Assert.NotNull(session.CurrentUser);

            var authzService = new AuthorizationService(session);
            var empRepo = new EmployeeRepository(db2);
            var astRepo = new AssetRepository(db2);
            var asmRepo = new AssignmentRepository(db2);
            var assignmentService = new AssignmentService(asmRepo, astRepo, empRepo, db2, authzService);

            // Create Employee and Asset offline
            await empRepo.AddAsync(new Employee(employeeId, "EMP-OFF-01", "Alice Vance", "alice@local.lan", "Field Engineering"));
            await astRepo.AddAsync(new Asset(assetId, "AST-OFF-99", "Panasonic Toughbook", "Field Laptop", "TB-4433"));

            // Create Assignment
            await assignmentService.AssignAssetAsync(employeeId, assetId);

            var activeAsset = await astRepo.GetByIdAsync(assetId);
            Assert.Equal(AssetStatus.Assigned, activeAsset!.Status);
        }

        // ── PHASE 3 (Restart 2): Verify Persistence Across Cold Start ──
        using (var db3 = CreateContext())
        {
            var astRepo = new AssetRepository(db3);
            var asmRepo = new AssignmentRepository(db3);
            var empRepo = new EmployeeRepository(db3);

            var employee = await empRepo.GetByIdAsync(employeeId);
            Assert.NotNull(employee);
            Assert.Equal("Alice Vance", employee.Name);

            var asset = await astRepo.GetByIdAsync(assetId);
            Assert.NotNull(asset);
            Assert.Equal(AssetStatus.Assigned, asset.Status);

            var assignments = await asmRepo.GetByAssetIdAsync(assetId);
            Assert.Single(assignments);
            Assert.Equal(AssignmentStatus.Active, assignments[0].Status);
            assignmentId = assignments[0].Id;
        }

        // ── PHASE 4 (Restart 3): Complete Return & Lifecycle Transitions ──
        using (var db4 = CreateContext())
        {
            var userRepo = new UserRepository(db4);
            var session = new SessionService();
            var authService = new AuthenticationService(userRepo, passwordHasher, session);
            await authService.LoginAsync("offline_admin", "SecurePassword123!");

            var authzService = new AuthorizationService(session);
            var empRepo = new EmployeeRepository(db4);
            var astRepo = new AssetRepository(db4);
            var asmRepo = new AssignmentRepository(db4);
            var assignmentService = new AssignmentService(asmRepo, astRepo, empRepo, db4, authzService);

            // Return the asset
            await assignmentService.ReturnAssetAsync(assignmentId);

            // Transition asset to maintenance
            var asset = await astRepo.GetByIdAsync(assetId);
            Assert.NotNull(asset);
            Assert.Equal(AssetStatus.Available, asset.Status);

            asset.MarkMaintenance();
            await astRepo.UpdateAsync(asset);
        }

        // ── PHASE 5 (Restart 4): Final Verification & Aggregations ──
        using (var db5 = CreateContext())
        {
            var astRepo = new AssetRepository(db5);
            var asmRepo = new AssignmentRepository(db5);
            var empRepo = new EmployeeRepository(db5);
            var userRepo = new UserRepository(db5);

            var users = await userRepo.GetAllAsync();
            Assert.Single(users);
            Assert.Equal("offline_admin", users[0].Username);

            var employees = await empRepo.GetAllAsync();
            Assert.Single(employees);

            var asset = await astRepo.GetByIdAsync(assetId);
            Assert.NotNull(asset);
            Assert.Equal(AssetStatus.Maintenance, asset.Status);

            var assignment = await asmRepo.GetByIdAsync(assignmentId);
            Assert.NotNull(assignment);
            Assert.Equal(AssignmentStatus.Returned, assignment.Status);
            Assert.NotNull(assignment.ReturnedAt);
        }
    }

    public void Dispose()
    {
        // Clean up temporary physical database file
        if (File.Exists(_dbFilePath))
        {
            try
            {
                File.Delete(_dbFilePath);
            }
            catch
            {
                // File lock cleanup safety
            }
        }
    }
}