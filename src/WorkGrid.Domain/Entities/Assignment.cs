using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Entities;

public sealed class Assignment
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid AssetId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public AssignmentStatus Status { get; private set; }

    public Assignment(
        Guid id,
        Guid employeeId,
        Guid assetId,
        DateTimeOffset assignedAt,
        DateTimeOffset? returnedAt = null,
        AssignmentStatus status = AssignmentStatus.Active)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Assignment ID cannot be empty.");
        }

        if (employeeId == Guid.Empty)
        {
            throw new DomainValidationException("Employee ID cannot be empty in an assignment.");
        }

        if (assetId == Guid.Empty)
        {
            throw new DomainValidationException("Asset ID cannot be empty in an assignment.");
        }

        if (returnedAt.HasValue && returnedAt.Value < assignedAt)
        {
            throw new DomainValidationException("Returned date cannot be earlier than assigned date.");
        }

        Id = id;
        EmployeeId = employeeId;
        AssetId = assetId;
        AssignedAt = assignedAt;
        ReturnedAt = returnedAt;
        Status = status;
    }

    public void CompleteReturn(DateTimeOffset returnedAt)
    {
        if (returnedAt < AssignedAt)
        {
            throw new DomainValidationException("Returned date cannot be earlier than assigned date.");
        }

        ReturnedAt = returnedAt;
        Status = AssignmentStatus.Returned;
    }
}
