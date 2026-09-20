using WorkGrid.App.ViewModels.Assignments;

namespace WorkGrid.App.Views.Assignments;

public partial class AssignmentListPage : ContentPage
{
    private readonly AssignmentListViewModel _viewModel;

    public AssignmentListPage(AssignmentListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAssignmentsAsync();
    }
}
