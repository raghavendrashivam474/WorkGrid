using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;
using WorkGrid.Infrastructure.Repositories;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class AssignmentWorkflowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WorkGridDbContext _context;
    private readonly EmployeeRepository _employeeRepo;
    private readonly AssetRepository _assetRepo;
    private readonly AssignmentRepository _assignmentRepo;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authzService;
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

        _sessionService = new SessionService();
        _sessionService.StartSession(new User(Guid.NewGuid(), "admin", "hash", "Admin", UserRole.Admin));
        _authzService = new AuthorizationService(_sessionService);

        _assignmentService = new AssignmentService(_assignmentRepo, _assetRepo, _employeeRepo, _context, _authzService);
    }

    [Fact]
    public async Task AssignAssetAsync_WithValidData_CreatesAssignmentAndMarksAssetAssigned()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP-001", "John Doe", "john@example.com");
        var asset = new Asset(Guid.NewGuid(), "AST-001", "MacBook Pro", "Laptop", "SN123", AssetStatus.Available);

        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await _assignmentService.AssignAssetAsync(employee.Id, asset.Id);

        var updatedAsset = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatus.Assigned, updatedAsset.Status);

        var assignments = await _assignmentRepo.GetByAssetIdAsync(asset.Id);
        var activeAssignment = assignments.FirstOrDefault(a => a.Status == AssignmentStatus.Active);
        Assert.NotNull(activeAssignment);
        Assert.Equal(employee.Id, activeAssignment.EmployeeId);
        Assert.Equal(AssignmentStatus.Active, activeAssignment.Status);
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetAlreadyAssigned_ThrowsDomainValidationException()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP-002", "Jane Doe", "jane@example.com");
        var asset = new Asset(Guid.NewGuid(), "AST-002", "Dell XPS", "Laptop", "SN456", AssetStatus.Assigned);

        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        var assignment = new Assignment(Guid.NewGuid(), employee.Id, asset.Id, DateTimeOffset.UtcNow);
        await _assignmentRepo.AddAsync(assignment);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.AssignAssetAsync(employee.Id, asset.Id));
    }

    [Fact]
    public async Task AssignAssetAsync_WhenAssetInMaintenance_ThrowsDomainValidationException()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP-003", "Bob Smith", "bob@example.com");
        var asset = new Asset(Guid.NewGuid(), "AST-003", "iPad Pro", "Tablet", "SN789", AssetStatus.Maintenance);

        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.AssignAssetAsync(employee.Id, asset.Id));
    }

    [Fact]
    public async Task ReturnAssetAsync_WithActiveAssignment_MarksReturnedAndAssetAvailable()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP-004", "Alice Smith", "alice@example.com");
        var asset = new Asset(Guid.NewGuid(), "AST-004", "ThinkPad", "Laptop", "SN101", AssetStatus.Assigned);
        var assignment = new Assignment(Guid.NewGuid(), employee.Id, asset.Id, DateTimeOffset.UtcNow.AddDays(-5));

        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);
        await _assignmentRepo.AddAsync(assignment);

        await _assignmentService.ReturnAssetAsync(assignment.Id);

        var updatedAssignment = await _assignmentRepo.GetByIdAsync(assignment.Id);
        Assert.NotNull(updatedAssignment);
        Assert.Equal(AssignmentStatus.Returned, updatedAssignment.Status);
        Assert.NotNull(updatedAssignment.ReturnedAt);

        var updatedAsset = await _assetRepo.GetByIdAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.Equal(AssetStatus.Available, updatedAsset.Status);
    }

    [Fact]
    public async Task ReturnAssetAsync_WhenAlreadyReturned_ThrowsDomainValidationException()
    {
        var employee = new Employee(Guid.NewGuid(), "EMP-005", "Charlie Brown", "charlie@example.com");
        var asset = new Asset(Guid.NewGuid(), "AST-005", "Surface Pro", "Laptop", "SN202", AssetStatus.Available);
        var assignment = new Assignment(Guid.NewGuid(), employee.Id, asset.Id, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1), AssignmentStatus.Returned);

        await _employeeRepo.AddAsync(employee);
        await _assetRepo.AddAsync(asset);
        await _assignmentRepo.AddAsync(assignment);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _assignmentService.ReturnAssetAsync(assignment.Id));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
