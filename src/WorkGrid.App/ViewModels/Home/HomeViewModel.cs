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
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionService _sessionService;

    private string _title = "WorkGrid Dashboard";
    private bool _isLoading;
    private int _employeeCount;
    private int _assetCount;
    private int _availableAssetCount;
    private int _assignedAssetCount;
    private int _maintenanceAssetCount;
    private int _retiredAssetCount;
    private int _activeAssignmentCount;
    private string _currentUserName = string.Empty;
    private string _currentUserRole = string.Empty;

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

    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    public string CurrentUserRole
    {
        get => _currentUserRole;
        set => SetProperty(ref _currentUserRole, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand LogoutCommand { get; }

    public HomeViewModel(
        IEmployeeRepository employeeRepository,
        IAssetRepository assetRepository,
        IAssignmentRepository assignmentRepository,
        IAuthenticationService authenticationService,
        ISessionService sessionService)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        RefreshCommand = new Command(async () => await LoadDashboardAsync());
        LogoutCommand = new Command(async () => await ExecuteLogoutAsync());
    }

    public async Task LoadDashboardAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            if (_sessionService.CurrentUser is { } user)
            {
                CurrentUserName = user.DisplayName;
                CurrentUserRole = user.Role.ToString();
            }
            else
            {
                CurrentUserName = "User";
                CurrentUserRole = string.Empty;
            }

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

    private async Task ExecuteLogoutAsync()
    {
        _authenticationService.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}
