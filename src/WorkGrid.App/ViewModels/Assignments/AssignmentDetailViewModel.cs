using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.App.ViewModels.Assignments;

[QueryProperty(nameof(IdString), "id")]
public sealed class AssignmentDetailViewModel : ViewModelBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAuthorizationService _authorizationService;

    private Guid _id;
    private string? _idString;
    private string _title = "New Assignment";
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isEditMode;
    private bool _isActive;

    // Selection sources (for Create mode)
    public ObservableCollection<Employee> Employees { get; } = new();
    public ObservableCollection<Asset> AvailableAssets { get; } = new();

    // Selected items (for Create mode)
    private Employee? _selectedEmployee;
    private Asset? _selectedAsset;

    // Information (for View mode)
    private string _employeeCode = string.Empty;
    private string _employeeName = string.Empty;
    private string _assetTag = string.Empty;
    private string _assetName = string.Empty;
    private DateTimeOffset _assignedAt;
    private DateTimeOffset? _returnedAt;

    public string? IdString
    {
        get => _idString;
        set
        {
            _idString = value;
            if (Guid.TryParse(value, out var parsedId) && parsedId != Guid.Empty)
            {
                _id = parsedId;
                IsEditMode = true;
                Title = "Assignment Details";
                MainThread.BeginInvokeOnMainThread(async () => await LoadAssignmentAsync());
            }
            else
            {
                _id = Guid.Empty;
                IsEditMode = false;
                Title = "New Assignment";
                IsActive = false;
                SelectedEmployee = null;
                SelectedAsset = null;
                MainThread.BeginInvokeOnMainThread(async () => await LoadSelectionSourcesAsync());
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public Employee? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetProperty(ref _selectedEmployee, value);
    }

    public Asset? SelectedAsset
    {
        get => _selectedAsset;
        set => SetProperty(ref _selectedAsset, value);
    }

    public string EmployeeCode
    {
        get => _employeeCode;
        set => SetProperty(ref _employeeCode, value);
    }

    public string EmployeeName
    {
        get => _employeeName;
        set => SetProperty(ref _employeeName, value);
    }

    public string AssetTag
    {
        get => _assetTag;
        set => SetProperty(ref _assetTag, value);
    }

    public string AssetName
    {
        get => _assetName;
        set => SetProperty(ref _assetName, value);
    }

    public DateTimeOffset AssignedAt
    {
        get => _assignedAt;
        set => SetProperty(ref _assignedAt, value);
    }

    public DateTimeOffset? ReturnedAt
    {
        get => _returnedAt;
        set => SetProperty(ref _returnedAt, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ReturnCommand { get; }
    public ICommand CancelCommand { get; }

    public AssignmentDetailViewModel(
        IAssignmentService assignmentService,
        IAssignmentRepository assignmentRepository,
        IEmployeeRepository employeeRepository,
        IAssetRepository assetRepository,
        IAuthorizationService authorizationService)
    {
        _assignmentService = assignmentService ?? throw new ArgumentNullException(nameof(assignmentService));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        SaveCommand = new Command(async () => await SaveAsync());
        ReturnCommand = new Command(async () => await ReturnAsync());
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task LoadAssignmentAsync()
    {
        if (_id == Guid.Empty) return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var asm = await _assignmentRepository.GetByIdAsync(_id);
            if (asm != null)
            {
                AssignedAt = asm.AssignedAt;
                ReturnedAt = asm.ReturnedAt;
                IsActive = asm.Status == AssignmentStatus.Active;

                var emp = await _employeeRepository.GetByIdAsync(asm.EmployeeId);
                EmployeeCode = emp?.EmployeeCode ?? "N/A";
                EmployeeName = emp?.Name ?? "Unknown Employee";

                var ast = await _assetRepository.GetByIdAsync(asm.AssetId);
                AssetTag = ast?.AssetTag ?? "N/A";
                AssetName = ast?.Name ?? "Unknown Asset";
            }
            else
            {
                ErrorMessage = "Assignment not found.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load details: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadSelectionSourcesAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var employees = await _employeeRepository.GetAllAsync();
            var assets = await _assetRepository.GetAllAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Employees.Clear();
                foreach (var emp in employees)
                {
                    Employees.Add(emp);
                }

                AvailableAssets.Clear();
                foreach (var ast in assets)
                {
                    if (ast.Status == AssetStatus.Available)
                    {
                        AvailableAssets.Add(ast);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load setup data: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy || IsEditMode) return;

        ErrorMessage = string.Empty;

        if (SelectedEmployee is null)
        {
            ErrorMessage = "Please select an employee.";
            return;
        }

        if (SelectedAsset is null)
        {
            ErrorMessage = "Please select an available asset.";
            return;
        }

        IsBusy = true;

        try
        {
            _authorizationService.EnsurePermission(AppPermission.AssignmentCreate);

            await _assignmentService.AssignAssetAsync(SelectedEmployee.Id, SelectedAsset.Id);
            await Shell.Current.GoToAsync("..");
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create assignment: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReturnAsync()
    {
        if (IsBusy || !IsEditMode || _id == Guid.Empty) return;

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            _authorizationService.EnsurePermission(AppPermission.AssignmentReturn);

            await _assignmentService.ReturnAssetAsync(_id);
            await LoadAssignmentAsync(); // Reload state
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to process return: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
