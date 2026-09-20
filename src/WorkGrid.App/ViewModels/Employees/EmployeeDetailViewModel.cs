using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.App.ViewModels.Common;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.App.ViewModels.Employees;

[QueryProperty(nameof(IdString), "id")]
public sealed class EmployeeDetailViewModel : ViewModelBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAssetRepository _assetRepository;

    private Guid _id;
    private string? _idString;
    private string _employeeCode = string.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string? _department;
    private string _title = "New Employee";
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isEditMode;
    private bool _canDelete;

    public ObservableCollection<AssignmentHistoryItem> ActiveAssignments { get; } = new();
    public ObservableCollection<AssignmentHistoryItem> HistoryAssignments { get; } = new();

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
                Title = "Edit Employee";
                Task.Run(LoadEmployeeAsync);
            }
            else
            {
                _id = Guid.Empty;
                IsEditMode = false;
                Title = "New Employee";
                CanDelete = false;
                ActiveAssignments.Clear();
                HistoryAssignments.Clear();
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string EmployeeCode
    {
        get => _employeeCode;
        set => SetProperty(ref _employeeCode, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
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

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }

    public EmployeeDetailViewModel(
        IEmployeeRepository employeeRepository,
        IAssignmentRepository assignmentRepository,
        IAssetRepository assetRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));

        SaveCommand = new Command(async () => await SaveAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task LoadEmployeeAsync()
    {
        if (_id == Guid.Empty) return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var emp = await _employeeRepository.GetByIdAsync(_id);
            if (emp != null)
            {
                EmployeeCode = emp.EmployeeCode;
                Name = emp.Name;
                Email = emp.Email;
                Department = emp.Department;
                CanDelete = true;

                // Load relational assignments
                var rawAssignments = await _assignmentRepository.GetByEmployeeIdAsync(_id);
                var assets = await _assetRepository.GetAllAsync();
                var assetMap = assets.ToDictionary(a => a.Id);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ActiveAssignments.Clear();
                    HistoryAssignments.Clear();

                    foreach (var asm in rawAssignments)
                    {
                        assetMap.TryGetValue(asm.AssetId, out var ast);
                        var item = new AssignmentHistoryItem
                        {
                            AssignmentId = asm.Id,
                            ReferenceId = asm.AssetId,
                            Title = $"{ast?.AssetTag ?? "N/A"} — {ast?.Name ?? "Unknown Asset"}",
                            Subtitle = ast?.AssetType ?? "Asset",
                            AssignedAt = asm.AssignedAt,
                            ReturnedAt = asm.ReturnedAt
                        };

                        if (asm.Status == AssignmentStatus.Active)
                            ActiveAssignments.Add(item);
                        else
                            HistoryAssignments.Add(item);
                    }
                });
            }
            else
            {
                ErrorMessage = "Employee not found.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load employee: {ex.Message}";
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

        if (string.IsNullOrWhiteSpace(EmployeeCode) && !IsEditMode)
        {
            ErrorMessage = "Employee code is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Employee name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@', StringComparison.Ordinal))
        {
            ErrorMessage = "A valid email address is required.";
            return;
        }

        IsBusy = true;

        try
        {
            if (IsEditMode)
            {
                var existing = await _employeeRepository.GetByIdAsync(_id);
                if (existing == null)
                {
                    ErrorMessage = "Employee no longer exists.";
                    return;
                }

                existing.UpdateDetails(Name, Email, Department);
                await _employeeRepository.UpdateAsync(existing);
            }
            else
            {
                var exists = await _employeeRepository.ExistsByCodeAsync(EmployeeCode);
                if (exists)
                {
                    ErrorMessage = $"Employee code '{EmployeeCode.Trim()}' is already in use.";
                    return;
                }

                var newEmployee = new Employee(
                    Guid.NewGuid(),
                    EmployeeCode,
                    Name,
                    Email,
                    Department);

                await _employeeRepository.AddAsync(newEmployee);
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

    private async Task DeleteAsync()
    {
        if (IsBusy || !IsEditMode || _id == Guid.Empty) return;

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var hasActive = await _assignmentRepository.HasActiveAssignmentsForEmployeeAsync(_id);
            if (hasActive)
            {
                ErrorMessage = "Cannot delete employee with active asset assignments. Return all assigned assets first.";
                return;
            }

            var existing = await _employeeRepository.GetByIdAsync(_id);
            if (existing != null)
            {
                await _employeeRepository.DeleteAsync(existing);
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
