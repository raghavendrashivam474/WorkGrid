using WorkGrid.App.ViewModels.Employees;

namespace WorkGrid.App.Views.Employees;

public partial class EmployeeListPage : ContentPage
{
    private readonly EmployeeListViewModel _viewModel;

    public EmployeeListPage(EmployeeListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadEmployeesAsync();
    }
}
