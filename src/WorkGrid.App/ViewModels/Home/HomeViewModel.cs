using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Enums;

namespace WorkGrid.App.ViewModels.Home;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssignmentRepository _assignmentRepository;

    private string _title = "WorkGrid Dashboard";
    private bool _isLoading;
    private int _employeeCount;
    private int _assetCount;
    private int _availableAssetCount;
    private int _assignedAssetCount;
    private int _maintenanceAssetCount;
    private int _retiredAssetCount;
    private int _activeAssignmentCount;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public int EmployeeCount
    {
        get => _employeeCount;
        set => SetProperty(ref _employeeCount, value);
    }

    public int AssetCount
    {
        get => _assetCount;
        set => SetProperty(ref _assetCount, value);
    }

    public int AvailableAssetCount
    {
        get => _availableAssetCount;
        set => SetProperty(ref _availableAssetCount, value);
    }

    public int AssignedAssetCount
    {
        get => _assignedAssetCount;
        set => SetProperty(ref _assignedAssetCount, value);
    }

    public int MaintenanceAssetCount
    {
        get => _maintenanceAssetCount;
        set => SetProperty(ref _maintenanceAssetCount, value);
    }

    public int RetiredAssetCount
    {
        get => _retiredAssetCount;
        set => SetProperty(ref _retiredAssetCount, value);
    }

    public int ActiveAssignmentCount
    {
        get => _activeAssignmentCount;
        set => SetProperty(ref _activeAssignmentCount, value);
    }

    public ICommand RefreshCommand { get; }

    public HomeViewModel(
        IEmployeeRepository employeeRepository,
        IAssetRepository assetRepository,
        IAssignmentRepository assignmentRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));

        RefreshCommand = new Command(async () => await LoadDashboardAsync());
    }

    public async Task LoadDashboardAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            var employees = await _employeeRepository.GetAllAsync();
            var assets = await _assetRepository.GetAllAsync();
            var assignments = await _assignmentRepository.GetAllAsync();

            EmployeeCount = employees.Count;
            AssetCount = assets.Count;

            AvailableAssetCount = assets.Count(a => a.Status == AssetStatus.Available);
            AssignedAssetCount = assets.Count(a => a.Status == AssetStatus.Assigned);
            MaintenanceAssetCount = assets.Count(a => a.Status == AssetStatus.Maintenance);
            RetiredAssetCount = assets.Count(a => a.Status == AssetStatus.Retired);

            ActiveAssignmentCount = assignments.Count(a => a.Status == AssignmentStatus.Active);
        }
        catch
        {
            // Silently complete gracefully
        }
        finally
        {
            IsLoading = false;
        }
    }
}
