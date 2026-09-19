using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;

namespace WorkGrid.App.ViewModels.Assets;

public sealed class AssetListViewModel : ViewModelBase
{
    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusText = "No assets registered yet.";

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand AddAssetCommand { get; }
    public ICommand ViewDetailsCommand { get; }

    public AssetListViewModel()
    {
        AddAssetCommand = new Command(async () => await GoToDetailsAsync(Guid.Empty));
        ViewDetailsCommand = new Command<Guid>(async (id) => await GoToDetailsAsync(id));
    }

    private async Task GoToDetailsAsync(Guid id)
    {
        await Shell.Current.GoToAsync($"asset-detail?id={id}");
    }
}
