using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.Domain.Tests;

public class AssignmentTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesActiveAssignment()
    {
        var id = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var assignment = new Assignment(id, empId, assetId, now);

        Assert.Equal(id, assignment.Id);
        Assert.Equal(empId, assignment.EmployeeId);
        Assert.Equal(assetId, assignment.AssetId);
        Assert.Equal(now, assignment.AssignedAt);
        Assert.Null(assignment.ReturnedAt);
        Assert.Equal(AssignmentStatus.Active, assignment.Status);
    }

    [Fact]
    public void Constructor_WithEmptyEmployeeId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Assignment(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WithEmptyAssetId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Assignment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WithReturnedBeforeAssigned_ThrowsDomainValidationException()
    {
        var now = DateTimeOffset.UtcNow;
        var past = now.AddDays(-1);

        Assert.Throws<DomainValidationException>(() =>
            new Assignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now, returnedAt: past));
    }

    [Fact]
    public void CompleteReturn_SetsReturnedDateAndStatus()
    {
        var assignedAt = DateTimeOffset.UtcNow.AddDays(-7);
        var returnedAt = DateTimeOffset.UtcNow;
        var assignment = new Assignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), assignedAt);

        assignment.CompleteReturn(returnedAt);

        Assert.Equal(returnedAt, assignment.ReturnedAt);
        Assert.Equal(AssignmentStatus.Returned, assignment.Status);
    }

    [Fact]
    public void CompleteReturn_WithEarlierDateThanAssigned_ThrowsDomainValidationException()
    {
        var assignedAt = DateTimeOffset.UtcNow;
        var assignment = new Assignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), assignedAt);

        Assert.Throws<DomainValidationException>(() =>
            assignment.CompleteReturn(assignedAt.AddHours(-1)));
    }
}
