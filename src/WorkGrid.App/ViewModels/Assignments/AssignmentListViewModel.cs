using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;

namespace WorkGrid.App.ViewModels.Assignments;

public sealed class AssignmentListViewModel : ViewModelBase
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAssetRepository _assetRepository;

    private bool _isLoading;
    private bool _isNavigating;
    private bool _isEmpty;
    private string _statusMessage = string.Empty;

    public ObservableCollection<AssignmentDisplayItem> Assignments { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((Command)LoadAssignmentsCommand).ChangeCanExecute();
                ((Command)AddAssignmentCommand).ChangeCanExecute();
                ((Command)SelectAssignmentCommand).ChangeCanExecute();
            }
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadAssignmentsCommand { get; }
    public ICommand AddAssignmentCommand { get; }
    public ICommand SelectAssignmentCommand { get; }

    public AssignmentListViewModel(
        IAssignmentRepository assignmentRepository,
        IEmployeeRepository employeeRepository,
        IAssetRepository assetRepository)
    {
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));

        LoadAssignmentsCommand = new Command(async () => await LoadAssignmentsAsync(), () => !IsLoading);
        AddAssignmentCommand = new Command(async () => await ExecuteAddAssignmentAsync(), () => !IsLoading && !_isNavigating);
        SelectAssignmentCommand = new Command<AssignmentDisplayItem>(async (item) => await ExecuteSelectAssignmentAsync(item), (item) => !IsLoading && !_isNavigating);
    }

    private async Task ExecuteAddAssignmentAsync()
    {
        if (_isNavigating || IsLoading) return;

        try
        {
            _isNavigating = true;
            ((Command)AddAssignmentCommand).ChangeCanExecute();
            ((Command)SelectAssignmentCommand).ChangeCanExecute();

            await Shell.Current.GoToAsync("assignment-detail");
        }
        finally
        {
            _isNavigating = false;
            ((Command)AddAssignmentCommand).ChangeCanExecute();
            ((Command)SelectAssignmentCommand).ChangeCanExecute();
        }
    }

    private async Task ExecuteSelectAssignmentAsync(AssignmentDisplayItem item)
    {
        if (item == null || _isNavigating || IsLoading) return;

        try
        {
            _isNavigating = true;
            ((Command)AddAssignmentCommand).ChangeCanExecute();
            ((Command)SelectAssignmentCommand).ChangeCanExecute();

            await Shell.Current.GoToAsync($"assignment-detail?id={item.Id}");
        }
        finally
        {
            _isNavigating = false;
            ((Command)AddAssignmentCommand).ChangeCanExecute();
            ((Command)SelectAssignmentCommand).ChangeCanExecute();
        }
    }

    public async Task LoadAssignmentsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = "Loading assignments...";

        try
        {
            var rawAssignments = await _assignmentRepository.GetAllAsync();
            var employees = await _employeeRepository.GetAllAsync();
            var assets = await _assetRepository.GetAllAsync();

            var empMap = employees.ToDictionary(e => e.Id);
            var assetMap = assets.ToDictionary(a => a.Id);

            Assignments.Clear();

            foreach (var asm in rawAssignments)
            {
                empMap.TryGetValue(asm.EmployeeId, out var emp);
                assetMap.TryGetValue(asm.AssetId, out var ast);

                Assignments.Add(new AssignmentDisplayItem
                {
                    Id = asm.Id,
                    EmployeeId = asm.EmployeeId,
                    EmployeeName = emp?.Name ?? "Unknown Employee",
                    EmployeeCode = emp?.EmployeeCode ?? "N/A",
                    AssetId = asm.AssetId,
                    AssetName = ast?.Name ?? "Unknown Asset",
                    AssetTag = ast?.AssetTag ?? "N/A",
                    AssignedAt = asm.AssignedAt,
                    ReturnedAt = asm.ReturnedAt,
                    Status = asm.Status.ToString()
                });
            }

            IsEmpty = Assignments.Count == 0;
            StatusMessage = IsEmpty ? "No assignments recorded yet. Tap '+' to create one." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}