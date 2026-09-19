using WorkGrid.App.ViewModels.Employees;

namespace WorkGrid.App.Views.Employees;

public partial class EmployeeListPage : ContentPage
{
    public EmployeeListPage(EmployeeListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
