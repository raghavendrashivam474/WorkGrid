using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;

namespace WorkGrid.App.ViewModels.Employees;

public sealed class EmployeeListViewModel : ViewModelBase
{
    private readonly IEmployeeRepository _repository;
    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusMessage = string.Empty;

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

    public ICommand LoadEmployeesCommand { get; }
    public ICommand AddEmployeeCommand { get; }
    public ICommand SelectEmployeeCommand { get; }

    public EmployeeListViewModel(IEmployeeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        LoadEmployeesCommand = new Command(async () => await LoadEmployeesAsync());
        AddEmployeeCommand = new Command(async () => await Shell.Current.GoToAsync("employee-detail"));
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
            Employees.Clear();
            foreach (var emp in list)
            {
                Employees.Add(emp);
            }

            IsEmpty = Employees.Count == 0;
            StatusMessage = IsEmpty ? "No employees found. Tap '+' to create one." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading employees: {ex.Message}";
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
