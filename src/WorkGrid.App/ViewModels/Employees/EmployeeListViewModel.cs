using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;

namespace WorkGrid.App.ViewModels.Employees;

public sealed class EmployeeListViewModel : ViewModelBase
{
    private readonly IEmployeeRepository _repository;
    private readonly IAuthorizationService _authorizationService;
    private readonly List<Employee> _allEmployees = new();

    private bool _isLoading;
    private bool _isNavigating;
    private bool _isEmpty = true;
    private string _statusMessage = string.Empty;
    private string _searchText = string.Empty;

    public ObservableCollection<Employee> Employees { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                ((Command)LoadEmployeesCommand).ChangeCanExecute();
                ((Command)AddEmployeeCommand).ChangeCanExecute();
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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool CanCreateEmployee => _authorizationService.HasPermission(AppPermission.EmployeeCreate);

    public ICommand LoadEmployeesCommand { get; }
    public ICommand AddEmployeeCommand { get; }
    public ICommand SelectEmployeeCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public EmployeeListViewModel(
        IEmployeeRepository repository,
        IAuthorizationService authorizationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        LoadEmployeesCommand = new Command(async () => await LoadEmployeesAsync(), () => !IsLoading);
        AddEmployeeCommand = new Command(async () => await ExecuteAddEmployeeAsync(), () => !IsLoading && !_isNavigating && CanCreateEmployee);
        ClearSearchCommand = new Command(() => SearchText = string.Empty);
        SelectEmployeeCommand = new Command<Employee>(async (emp) => await ExecuteSelectEmployeeAsync(emp));
    }

    public async Task LoadEmployeesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = "Loading employees...";

        try
        {
            var list = await _repository.GetAllAsync();
            _allEmployees.Clear();
            _allEmployees.AddRange(list);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading employees: {ex.Message}";
            Employees.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteAddEmployeeAsync()
    {
        if (_isNavigating || IsLoading) return;

        try
        {
            _isNavigating = true;
            await Shell.Current.GoToAsync("employee-detail");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async Task ExecuteSelectEmployeeAsync(Employee? emp)
    {
        if (emp is null || _isNavigating || IsLoading) return;

        try
        {
            _isNavigating = true;
            await Shell.Current.GoToAsync($"employee-detail?id={emp.Id}");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void ApplyFilter()
    {
        var query = _allEmployees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.EmployeeCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Department != null && e.Department.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        Employees.Clear();
        foreach (var emp in query)
        {
            Employees.Add(emp);
        }

        IsEmpty = Employees.Count == 0;
        if (IsEmpty)
        {
            StatusMessage = string.IsNullOrWhiteSpace(SearchText)
                ? "No employees found. Tap '+ Add Employee' to create one."
                : $"No employees match '{SearchText.Trim()}'.";
        }
        else
        {
            StatusMessage = string.Empty;
        }
    }
}
