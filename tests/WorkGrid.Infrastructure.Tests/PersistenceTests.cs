using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;

namespace WorkGrid.Infrastructure.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WorkGridDbContext _context;
    private readonly EmployeeRepository _employeeRepo;
    private readonly AssetRepository _assetRepo;
    private readonly AssignmentRepository _assignmentRepo;

    public PersistenceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.EnsureCreated();

        _employeeRepo = new EmployeeRepository(_context);
        _assetRepo = new AssetRepository(_context);
        _assignmentRepo = new AssignmentRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task EmployeeRepository_AddAndRetrieve_WorksCorrectly()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP001", "Alice Smith", "alice@example.com", "Engineering");

        await _employeeRepo.AddAsync(employee);

        var retrieved = await _employeeRepo.GetByIdAsync(employee.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("EMP001", retrieved.EmployeeCode);
        Assert.Equal("Alice Smith", retrieved.Name);
        Assert.Equal("alice@example.com", retrieved.Email);
        Assert.Equal("Engineering", retrieved.Department);
    }

    [Fact]
    public async Task EmployeeRepository_UpdateAndGetAll_ReflectsChanges()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP002", "Bob Jones", "bob@example.com", "Operations");
        await _employeeRepo.AddAsync(employee);

        employee.UpdateDetails("Robert Jones", "robert@example.com", "Logistics");
        await _employeeRepo.UpdateAsync(employee);

        var all = await _employeeRepo.GetAllAsync();
        var retrieved = Assert.Single(all, e => e.Id == employee.Id);
        Assert.Equal("Robert Jones", retrieved.Name);
        Assert.Equal("robert@example.com", retrieved.Email);
        Assert.Equal("Logistics", retrieved.Department);
    }

    [Fact]
    public async Task EmployeeRepository_Delete_RemovesRecord()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP003", "Carol White", "carol@example.com");
        await _employeeRepo.AddAsync(employee);

        await _employeeRepo.DeleteAsync(employee);

        var retrieved = await _employeeRepo.GetByIdAsync(employee.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task AssetRepository_AddAndCheckLifecyclePersistence_WorksCorrectly()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-100", "MacBook Pro", "Laptop", "MBP-9921", AssetStatus.Available);
        await _assetRepo.AddAsync(asset);

        // Transition state
        asset.MarkAssigned();
        await _assetRepo.UpdateAsync(asset);

        var retrieved = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(AssetStatus.Assigned, retrieved.Status);

        asset.Retire();
        await _assetRepo.UpdateAsync(asset);

        var retiredAsset = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(retiredAsset);
        Assert.Equal(AssetStatus.Retired, retiredAsset.Status);
    }

    [Fact]
    public async Task AssignmentRepository_ActiveCheckAndReturnLifecycle_WorksCorrectly()
    {
        var empId = Guid.NewGuid();
        var astId = Guid.NewGuid();

        var assignment = new Assignment(Guid.NewGuid(), empId, astId, DateTimeOffset.UtcNow);
        await _assignmentRepo.AddAsync(assignment);

        var hasActiveEmp = await _assignmentRepo.HasActiveAssignmentsForEmployeeAsync(empId);
        var hasActiveAst = await _assignmentRepo.HasActiveAssignmentsForAssetAsync(astId);
        Assert.True(hasActiveEmp);
        Assert.True(hasActiveAst);

        // Return asset
        assignment.CompleteReturn(DateTimeOffset.UtcNow.AddHours(1));
        await _assignmentRepo.UpdateAsync(assignment);

        var hasActiveAfterReturn = await _assignmentRepo.HasActiveAssignmentsForEmployeeAsync(empId);
        Assert.False(hasActiveAfterReturn);

        var retrieved = await _assignmentRepo.GetByIdAsync(assignment.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(AssignmentStatus.Returned, retrieved.Status);
        Assert.NotNull(retrieved.ReturnedAt);
    }
}
