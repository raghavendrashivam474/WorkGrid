using WorkGrid.App.ViewModels.Assets;

namespace WorkGrid.App.Views.Assets;

public partial class AssetDetailPage : ContentPage
{
    public AssetDetailPage(AssetDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
