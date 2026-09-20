using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Persistence;

namespace WorkGrid.Infrastructure.Services;

public sealed class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly WorkGridDbContext? _context;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IAssetRepository assetRepository,
        IEmployeeRepository employeeRepository,
        IAuthorizationService authorizationService)
    {
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IAssetRepository assetRepository,
        IEmployeeRepository employeeRepository,
        WorkGridDbContext context,
        IAuthorizationService authorizationService)
        : this(assignmentRepository, assetRepository, employeeRepository, authorizationService)
    {
        _context = context;
    }

    public async Task AssignAssetAsync(
        Guid employeeId,
        Guid assetId,
        CancellationToken ct = default)
    {
        _authorizationService.EnsurePermission(AppPermission.AssignmentCreate);

        var employee = await _employeeRepository.GetByIdAsync(employeeId, ct);
        if (employee is null)
        {
            throw new DomainValidationException($"Employee with ID '{employeeId}' does not exist.");
        }

        var asset = await _assetRepository.GetByIdAsync(assetId, ct);
        if (asset is null)
        {
            throw new DomainValidationException($"Asset with ID '{assetId}' does not exist.");
        }

        if (asset.Status != AssetStatus.Available)
        {
            throw new DomainValidationException(
                $"Asset '{asset.AssetTag}' cannot be assigned because its current status is '{asset.Status}'. Only 'Available' assets can be assigned.");
        }

        var isAlreadyAssigned = await _assignmentRepository.HasActiveAssignmentsForAssetAsync(assetId, ct);
        if (isAlreadyAssigned)
        {
            throw new DomainValidationException(
                $"Asset '{asset.AssetTag}' is already assigned under an active assignment.");
        }

        var assignment = new Assignment(Guid.NewGuid(), employeeId, assetId, DateTimeOffset.UtcNow);
        asset.MarkAssigned();

        await _assetRepository.UpdateAsync(asset, ct);
        await _assignmentRepository.AddAsync(assignment, ct);
    }

    public async Task ReturnAssetAsync(
        Guid assignmentId,
        CancellationToken ct = default)
    {
        _authorizationService.EnsurePermission(AppPermission.AssignmentReturn);

        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, ct);
        if (assignment is null)
        {
            throw new DomainValidationException($"Assignment with ID '{assignmentId}' does not exist.");
        }

        if (assignment.Status == AssignmentStatus.Returned)
        {
            throw new DomainValidationException(
                $"Assignment '{assignmentId}' is already marked as returned.");
        }

        var asset = await _assetRepository.GetByIdAsync(assignment.AssetId, ct);
        if (asset is null)
        {
            throw new DomainValidationException(
                $"Asset with ID '{assignment.AssetId}' referenced in assignment '{assignmentId}' does not exist.");
        }

        assignment.CompleteReturn(DateTimeOffset.UtcNow);
        asset.MarkAvailable();

        await _assignmentRepository.UpdateAsync(assignment, ct);
        await _assetRepository.UpdateAsync(asset, ct);
    }
}
