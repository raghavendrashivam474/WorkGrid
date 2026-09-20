using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;

namespace WorkGrid.App.ViewModels.Assets;

public sealed class AssetListViewModel : ViewModelBase
{
    private readonly IAssetRepository _repository;
    private readonly List<Asset> _allAssets = new();

    private bool _isLoading;
    private bool _isEmpty = true;
    private string _statusMessage = string.Empty;
    private string _searchText = string.Empty;
    private string _selectedFilter = "All";

    public ObservableCollection<Asset> Assets { get; } = new();
    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "All",
        "Available",
        "Assigned",
        "Maintenance",
        "Retired"
    };

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public ICommand LoadAssetsCommand { get; }
    public ICommand AddAssetCommand { get; }
    public ICommand SelectAssetCommand { get; }
    public ICommand SetFilterCommand { get; }

    public AssetListViewModel(IAssetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        LoadAssetsCommand = new Command(async () => await LoadAssetsAsync());
        AddAssetCommand = new Command(async () => await Shell.Current.GoToAsync("asset-detail"));
        SetFilterCommand = new Command<string>((filter) =>
        {
            if (!string.IsNullOrEmpty(filter))
                SelectedFilter = filter;
        });
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
            _allAssets.Clear();
            _allAssets.AddRange(list);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading assets: {ex.Message}";
            Assets.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = _allAssets.AsEnumerable();

        // 1. Apply status filter
        if (SelectedFilter != "All" && Enum.TryParse<AssetStatus>(SelectedFilter, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        // 2. Apply search query
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(a =>
                a.AssetTag.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (a.AssetType != null && a.AssetType.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        Assets.Clear();
        foreach (var ast in query)
        {
            Assets.Add(ast);
        }

        IsEmpty = Assets.Count == 0;
        if (IsEmpty)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
                StatusMessage = $"No assets match '{SearchText.Trim()}'.";
            else if (SelectedFilter != "All")
                StatusMessage = $"No assets found with status '{SelectedFilter}'.";
            else
                StatusMessage = "No assets registered. Tap '+' to add one.";
        }
        else
        {
            StatusMessage = string.Empty;
        }
    }
}
