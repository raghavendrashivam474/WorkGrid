using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.App.ViewModels.Assets;

[QueryProperty(nameof(IdString), "id")]
public sealed class AssetDetailViewModel : ViewModelBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IAssignmentRepository _assignmentRepository;

    private Guid _id;
    private string? _idString;
    private string _assetTag = string.Empty;
    private string _name = string.Empty;
    private string? _assetType;
    private string? _serialNumber;
    private AssetStatus _status = AssetStatus.Available;
    private string _title = "New Asset";
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isEditMode;
    private bool _canDelete;
    private bool _isRetired;

    public string? IdString
    {
        get => _idString;
        set
        {
            _idString = value;
            if (Guid.TryParse(value, out var parsedId) && parsedId != Guid.Empty)
            {
                _id = parsedId;
                IsEditMode = true;
                Title = "Edit Asset";
                Task.Run(LoadAssetAsync);
            }
            else
            {
                _id = Guid.Empty;
                IsEditMode = false;
                Title = "New Asset";
                CanDelete = false;
                IsRetired = false;
                Status = AssetStatus.Available;
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string AssetTag
    {
        get => _assetTag;
        set => SetProperty(ref _assetTag, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? AssetType
    {
        get => _assetType;
        set => SetProperty(ref _assetType, value);
    }

    public string? SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(ref _serialNumber, value);
    }

    public AssetStatus Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            IsRetired = value == AssetStatus.Retired;
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public bool CanDelete
    {
        get => _canDelete;
        set => SetProperty(ref _canDelete, value);
    }

    public bool IsRetired
    {
        get => _isRetired;
        set => SetProperty(ref _isRetired, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand MarkAvailableCommand { get; }
    public ICommand MarkMaintenanceCommand { get; }
    public ICommand RetireCommand { get; }

    public AssetDetailViewModel(
        IAssetRepository assetRepository,
        IAssignmentRepository assignmentRepository)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));

        SaveCommand = new Command(async () => await SaveAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        MarkAvailableCommand = new Command(async () => await ChangeStatusAsync(a => a.MarkAvailable()));
        MarkMaintenanceCommand = new Command(async () => await ChangeStatusAsync(a => a.MarkMaintenance()));
        RetireCommand = new Command(async () => await ChangeStatusAsync(a => a.Retire()));
    }

    public async Task LoadAssetAsync()
    {
        if (_id == Guid.Empty) return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var ast = await _assetRepository.GetByIdAsync(_id);
            if (ast != null)
            {
                AssetTag = ast.AssetTag;
                Name = ast.Name;
                AssetType = ast.AssetType;
                SerialNumber = ast.SerialNumber;
                Status = ast.Status;
                CanDelete = true;
            }
            else
            {
                ErrorMessage = "Asset not found.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load asset: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(AssetTag) && !IsEditMode)
        {
            ErrorMessage = "Asset tag is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Asset name is required.";
            return;
        }

        IsBusy = true;

        try
        {
            if (IsEditMode)
            {
                var existing = await _assetRepository.GetByIdAsync(_id);
                if (existing == null)
                {
                    ErrorMessage = "Asset no longer exists.";
                    return;
                }

                existing.UpdateDetails(Name, AssetType, SerialNumber);
                await _assetRepository.UpdateAsync(existing);
            }
            else
            {
                var exists = await _assetRepository.ExistsByTagAsync(AssetTag);
                if (exists)
                {
                    ErrorMessage = $"Asset tag '{AssetTag.Trim().ToUpperInvariant()}' already exists.";
                    return;
                }

                var newAsset = new Asset(
                    Guid.NewGuid(),
                    AssetTag,
                    Name,
                    AssetType,
                    SerialNumber,
                    Status);

                await _assetRepository.AddAsync(newAsset);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangeStatusAsync(Action<Asset> transition)
    {
        if (IsBusy || !IsEditMode || _id == Guid.Empty) return;

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var existing = await _assetRepository.GetByIdAsync(_id);
            if (existing == null)
            {
                ErrorMessage = "Asset not found.";
                return;
            }

            transition(existing);
            await _assetRepository.UpdateAsync(existing);
            Status = existing.Status;
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to change status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (IsBusy || !IsEditMode || _id == Guid.Empty) return;

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var hasActive = await _assignmentRepository.HasActiveAssignmentsForAssetAsync(_id);
            if (hasActive)
            {
                ErrorMessage = "Cannot delete an asset with active assignments. Complete the return first.";
                return;
            }

            var existing = await _assetRepository.GetByIdAsync(_id);
            if (existing != null)
            {
                await _assetRepository.DeleteAsync(existing);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
