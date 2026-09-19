using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;

namespace WorkGrid.App.ViewModels.Employees;

public sealed class EmployeeListViewModel : ViewModelBase
{
    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusText = "No employees loaded yet.";

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

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand AddEmployeeCommand { get; }
    public ICommand ViewDetailsCommand { get; }

    public EmployeeListViewModel()
    {
        AddEmployeeCommand = new Command(async () => await GoToDetailsAsync(Guid.Empty));
        ViewDetailsCommand = new Command<Guid>(async (id) => await GoToDetailsAsync(id));
    }

    private async Task GoToDetailsAsync(Guid id)
    {
        await Shell.Current.GoToAsync($"employee-detail?id={id}");
    }
}
