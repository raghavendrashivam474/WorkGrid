using WorkGrid.App.ViewModels.Users;

namespace WorkGrid.App.Views.Users;

public partial class UserListPage : ContentPage
{
    private readonly UserListViewModel _viewModel;

    public UserListPage(UserListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
