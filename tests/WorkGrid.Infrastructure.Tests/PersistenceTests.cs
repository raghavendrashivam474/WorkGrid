using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WorkGridDbContext _context;
    private readonly EmployeeRepository _employeeRepository;
    private readonly AssetRepository _assetRepository;
    private readonly AssignmentRepository _assignmentRepository;

    public PersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.EnsureCreated();

        _employeeRepository = new EmployeeRepository(_context);
        _assetRepository = new AssetRepository(_context);
        _assignmentRepository = new AssignmentRepository(_context);
    }

    [Fact]
    public async Task EmployeeRepository_AddAndRetrieve_WorksCorrectly()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP-001",
            "John Doe",
            "john.doe@workgrid.local",
            "IT Operations");

        await _employeeRepository.AddAsync(employee);
        var retrieved = await _employeeRepository.GetByIdAsync(employee.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("EMP-001", retrieved.EmployeeCode);
        Assert.Equal("John Doe", retrieved.Name);
    }

    [Fact]
    public async Task EmployeeRepository_UpdateAndGetAll_ReflectsChanges()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP-002",
            "Jane Smith",
            "jane.smith@workgrid.local",
            "Finance");

        await _employeeRepository.AddAsync(employee);
        employee.UpdateDetails("Jane Smith-Doe", "jane.doe@workgrid.local", "Accounting");
        await _employeeRepository.UpdateAsync(employee);

        var all = await _employeeRepository.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Jane Smith-Doe", all[0].Name);
    }

    [Fact]
    public async Task EmployeeRepository_Delete_RemovesRecord()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP-003",
            "Bob Wilson",
            "bob@workgrid.local");

        await _employeeRepository.AddAsync(employee);
        await _employeeRepository.DeleteAsync(employee);

        var retrieved = await _employeeRepository.GetByIdAsync(employee.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task AssetRepository_AddAndCheckLifecyclePersistence_WorksCorrectly()
    {
        var asset = new Asset(
            Guid.NewGuid(),
            "AST-001",
            "Dell Latitude 5520",
            "Laptop",
            "SN-12345");

        await _assetRepository.AddAsync(asset);

        // Lifecycle transition: Available -> Assigned -> Available -> Retired
        asset.MarkAssigned();
        await _assetRepository.UpdateAsync(asset);

        var assigned = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.Equal(AssetStatus.Assigned, assigned!.Status);

        asset.MarkAvailable();
        asset.Retire();
        await _assetRepository.UpdateAsync(asset);

        var retired = await _assetRepository.GetByIdAsync(asset.Id);
        Assert.Equal(AssetStatus.Retired, retired!.Status);
    }

    [Fact]
    public async Task AssignmentRepository_ActiveCheckAndReturnLifecycle_WorksCorrectly()
    {
        var empId = Guid.NewGuid();
        var astId = Guid.NewGuid();
        var assignment = new Assignment(
            Guid.NewGuid(),
            empId,
            astId,
            DateTimeOffset.UtcNow);

        await _assignmentRepository.AddAsync(assignment);

        var hasActiveEmp = await _assignmentRepository.HasActiveAssignmentsForEmployeeAsync(empId);
        var hasActiveAst = await _assignmentRepository.HasActiveAssignmentsForAssetAsync(astId);

        Assert.True(hasActiveEmp);
        Assert.True(hasActiveAst);

        // Return assignment
        assignment.CompleteReturn(DateTimeOffset.UtcNow);
        await _assignmentRepository.UpdateAsync(assignment);

        var hasActiveAfterReturn = await _assignmentRepository.HasActiveAssignmentsForAssetAsync(astId);
        Assert.False(hasActiveAfterReturn);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
