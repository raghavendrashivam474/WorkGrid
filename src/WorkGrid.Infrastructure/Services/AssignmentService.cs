using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Services;

public sealed class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignments;
    private readonly IAssetRepository _assets;
    private readonly IEmployeeRepository _employees;
    private readonly WorkGridDbContext _db;

    public AssignmentService(
        IAssignmentRepository assignments,
        IAssetRepository assets,
        IEmployeeRepository employees,
        WorkGridDbContext db)
    {
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task AssignAssetAsync(Guid employeeId, Guid assetId, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(employeeId, ct);
        if (employee is null)
            throw new DomainValidationException("Employee not found.");

        var asset = await _assets.GetByIdAsync(assetId, ct);
        if (asset is null)
            throw new DomainValidationException("Asset not found.");

        if (await _assignments.HasActiveAssignmentsForAssetAsync(assetId, ct))
            throw new DomainValidationException("This asset already has an active assignment.");

        asset.MarkAssigned();

        var assignment = new Assignment(
            id: Guid.NewGuid(),
            employeeId: employeeId,
            assetId: assetId,
            assignedAt: DateTimeOffset.UtcNow,
            status: AssignmentStatus.Active);

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _assignments.AddAsync(assignment, ct);
            await _assets.UpdateAsync(asset, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ReturnAssetAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId, ct);
        if (assignment is null)
            throw new DomainValidationException("Assignment not found.");

        if (assignment.Status != AssignmentStatus.Active)
            throw new DomainValidationException("Only active assignments can be returned.");

        var asset = await _assets.GetByIdAsync(assignment.AssetId, ct);
        if (asset is null)
            throw new DomainValidationException("Asset not found for this assignment.");

        assignment.CompleteReturn(DateTimeOffset.UtcNow);
        asset.MarkAvailable();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _assignments.UpdateAsync(assignment, ct);
            await _assets.UpdateAsync(asset, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
