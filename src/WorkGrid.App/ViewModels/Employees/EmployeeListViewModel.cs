using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;

namespace WorkGrid.App.ViewModels.Employees;

public sealed class EmployeeListViewModel : ViewModelBase
{
    private readonly IEmployeeRepository _repository;
    private readonly List<Employee> _allEmployees = new();

    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusMessage = string.Empty;
    private string _searchText = string.Empty;

    public ObservableCollection<Employee> Employees { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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

    public ICommand LoadEmployeesCommand { get; }
    public ICommand AddEmployeeCommand { get; }
    public ICommand SelectEmployeeCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public EmployeeListViewModel(IEmployeeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        LoadEmployeesCommand = new Command(async () => await LoadEmployeesAsync());
        AddEmployeeCommand = new Command(async () => await Shell.Current.GoToAsync("employee-detail"));
        ClearSearchCommand = new Command(() => SearchText = string.Empty);
        SelectEmployeeCommand = new Command<Employee>(async (emp) =>
        {
            if (emp != null)
            {
                await Shell.Current.GoToAsync($"employee-detail?id={emp.Id}");
            }
        });
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
                ? "No employees found. Tap '+' to create one."
                : $"No employees match '{SearchText.Trim()}'.";
        }
        else
        {
            StatusMessage = string.Empty;
        }
    }
}
