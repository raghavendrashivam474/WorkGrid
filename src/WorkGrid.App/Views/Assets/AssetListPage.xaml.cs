using WorkGrid.App.ViewModels.Assets;

namespace WorkGrid.App.Views.Assets;

public partial class AssetListPage : ContentPage
{
    public AssetListPage(AssetListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
