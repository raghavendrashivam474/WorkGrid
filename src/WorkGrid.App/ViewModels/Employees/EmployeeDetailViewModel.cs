using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;

namespace WorkGrid.App.ViewModels.Employees;

[QueryProperty(nameof(IdString), "id")]
public sealed class EmployeeDetailViewModel : ViewModelBase
{
    private string? _idString;
    private string _title = "New Employee";

    public string? IdString
    {
        get => _idString;
        set
        {
            _idString = value;
            if (Guid.TryParse(value, out var parsedId) && parsedId != Guid.Empty)
            {
                Title = "Edit Employee";
            }
            else
            {
                Title = "New Employee";
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EmployeeDetailViewModel()
    {
        SaveCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }
}
