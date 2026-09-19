using WorkGrid.App.ViewModels.Assets;

namespace WorkGrid.App.Views.Assets;

public partial class AssetListPage : ContentPage
{
    private readonly AssetListViewModel _viewModel;

    public AssetListPage(AssetListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAssetsAsync();
    }
}
