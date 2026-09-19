using WorkGrid.App.ViewModels.Home;

namespace WorkGrid.App.Views.Home;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
