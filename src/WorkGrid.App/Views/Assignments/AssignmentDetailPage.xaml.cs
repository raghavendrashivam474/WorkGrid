using WorkGrid.App.ViewModels.Assignments;

namespace WorkGrid.App.Views.Assignments;

public partial class AssignmentDetailPage : ContentPage
{
    public AssignmentDetailPage(AssignmentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
