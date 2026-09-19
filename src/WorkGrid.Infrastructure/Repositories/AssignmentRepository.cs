using Microsoft.EntityFrameworkCore;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Repositories;

public sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly WorkGridDbContext _context;

    public AssignmentRepository(WorkGridDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AsNoTracking()
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.AssetId == assetId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveAssignmentsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AnyAsync(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active, cancellationToken);
    }

    public async Task<bool> HasActiveAssignmentsForAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AnyAsync(a => a.AssetId == assetId && a.Status == AssignmentStatus.Active, cancellationToken);
    }

    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.Assignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        _context.Assignments.Update(assignment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
