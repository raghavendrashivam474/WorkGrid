using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Infrastructure.Services;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class AssignmentWorkflowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WorkGridDbContext _context;
    private readonly EmployeeRepository _employeeRepo;
    private readonly AssetRepository _assetRepo;
    private readonly AssignmentRepository _assignmentRepo;
    private readonly AssignmentService _assignmentService;

    public AssignmentWorkflowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WorkGridDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WorkGridDbContext(options);
        _context.Database.EnsureCreated();

        _employeeRepo = new EmployeeRepository(_context);
        _assetRepo = new AssetRepository(_context);
        _assignmentRepo = new AssignmentRepository(_context);
        _assignmentService = new AssignmentService(_assignmentRepo, _assetRepo, _employeeRepo, _context);
    }

    [Fact]
    public async Task AssignAssetAsync_WithValidData_CreatesAssignmentAndMarksAssetAssigned()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP101", "Alice", "alice@test.com");
        var asset = new Asset(Guid.NewGuid(), "AST-101", "Laptop");
        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await _assignmentService.AssignAssetAsync(employee.Id, asset.Id);

        var updatedAsset = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatus.Assigned, updatedAsset.Status);

        var assignments = await _assignmentRepo.GetByAssetIdAsync(asset.Id);
        Assert.Single(assignments);
        Assert.Equal(AssignmentStatus.Active, assignments[0].Status);
        Assert.Equal(employee.Id, assignments[0].EmployeeId);
        Assert.Null(assignments[0].ReturnedAt);
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetAlreadyAssigned_ThrowsDomainValidationException()
    {
        var employee1 = new Employee(Guid.NewGuid(), "EMP101", "Alice", "alice@test.com");
        var employee2 = new Employee(Guid.NewGuid(), "EMP102", "Bob", "bob@test.com");
        var asset = new Asset(Guid.NewGuid(), "AST-101", "Laptop");
        await _employeeRepo.AddAsync(employee1);
        await _employeeRepo.AddAsync(employee2);
        await _assetRepo.AddAsync(asset);

        await _assignmentService.AssignAssetAsync(employee1.Id, asset.Id);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.AssignAssetAsync(employee2.Id, asset.Id));
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetInMaintenance_ThrowsDomainValidationException()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP101", "Alice", "alice@test.com");
        var asset = new Asset(Guid.NewGuid(), "AST-101", "Laptop", status: AssetStatus.Maintenance);
        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.AssignAssetAsync(employee.Id, asset.Id));
    }

    [Fact]
    public async Task ReturnAssetAsync_WithActiveAssignment_MarksReturnedAndAssetAvailable()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP101", "Alice", "alice@test.com");
        var asset = new Asset(Guid.NewGuid(), "AST-101", "Laptop");
        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await _assignmentService.AssignAssetAsync(employee.Id, asset.Id);
        var activeAssignments = await _assignmentRepo.GetByAssetIdAsync(asset.Id);
        var activeAssignmentId = activeAssignments[0].Id;

        await _assignmentService.ReturnAssetAsync(activeAssignmentId);

        var returnedAssignment = await _assignmentRepo.GetByIdAsync(activeAssignmentId);
        Assert.NotNull(returnedAssignment);
        Assert.Equal(AssignmentStatus.Returned, returnedAssignment.Status);
        Assert.NotNull(returnedAssignment.ReturnedAt);

        var updatedAsset = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatus.Available, updatedAsset.Status);
    }

    [Fact]
    public async Task ReturnAssetAsync_WhenAlreadyReturned_ThrowsDomainValidationException()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP101", "Alice", "alice@test.com");
        var asset = new Asset(Guid.NewGuid(), "AST-101", "Laptop");
        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await _assignmentService.AssignAssetAsync(employee.Id, asset.Id);
        var activeAssignments = await _assignmentRepo.GetByAssetIdAsync(asset.Id);
        var activeAssignmentId = activeAssignments[0].Id;

        await _assignmentService.ReturnAssetAsync(activeAssignmentId);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.ReturnAssetAsync(activeAssignmentId));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

