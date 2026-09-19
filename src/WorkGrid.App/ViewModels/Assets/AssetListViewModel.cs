using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;

namespace WorkGrid.App.ViewModels.Assets;

public sealed class AssetListViewModel : ViewModelBase
{
    private readonly IAssetRepository _repository;
    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusMessage = string.Empty;

    public ObservableCollection<Asset> Assets { get; } = new();

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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadAssetsCommand { get; }
    public ICommand AddAssetCommand { get; }
    public ICommand SelectAssetCommand { get; }

    public AssetListViewModel(IAssetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        LoadAssetsCommand = new Command(async () => await LoadAssetsAsync());
        AddAssetCommand = new Command(async () => await Shell.Current.GoToAsync("asset-detail"));
        SelectAssetCommand = new Command<Asset>(async (ast) =>
        {
            if (ast != null)
            {
                await Shell.Current.GoToAsync($"asset-detail?id={ast.Id}");
            }
        });
    }

    public async Task LoadAssetsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = "Loading assets...";

        try
        {
            var list = await _repository.GetAllAsync();
            Assets.Clear();
            foreach (var ast in list)
            {
                Assets.Add(ast);
            }

            IsEmpty = Assets.Count == 0;
            StatusMessage = IsEmpty ? "No assets registered. Tap '+' to add one." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading assets: {ex.Message}";
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
