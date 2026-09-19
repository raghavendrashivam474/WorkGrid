using WorkGrid.Domain.Entities;

namespace WorkGrid.Domain.Contracts;

public interface IAssignmentRepository
{
    Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignmentsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignmentsForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default);
}
