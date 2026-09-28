using WorkGrid.App.ViewModels.Assignments;

namespace WorkGrid.App.Views.Assignments;

public partial class AssignmentDetailPage : ContentPage
{
    private readonly AssignmentDetailViewModel _viewModel;

    public AssignmentDetailPage(AssignmentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.IsEditMode)
        {
            await _viewModel.LoadSelectionSourcesAsync();
        }
    }
}
