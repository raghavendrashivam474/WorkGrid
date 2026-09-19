using WorkGrid.App.ViewModels.Employees;

namespace WorkGrid.App.Views.Employees;

public partial class EmployeeDetailPage : ContentPage
{
    public EmployeeDetailPage(EmployeeDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
