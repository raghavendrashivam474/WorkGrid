namespace WorkGrid.Domain.Contracts;

/// <summary>
/// Service contract for atomic assignment operations spanning multiple aggregates.
/// </summary>
public interface IAssignmentService
{
    Task AssignAssetAsync(Guid employeeId, Guid assetId, CancellationToken ct = default);
    Task ReturnAssetAsync(Guid assignmentId, CancellationToken ct = default);
}
