using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;

namespace WorkGrid.App.ViewModels.Users;

public sealed class UserListViewModel : INotifyPropertyChanged
{
    private readonly IUserManagementService _userManagementService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISessionService _sessionService;

    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private string _newUsername = string.Empty;
    private string _newDisplayName = string.Empty;
    private string _newPassword = string.Empty;
    private UserRole _newRole = UserRole.Viewer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<User> Users { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

    public string NewUsername
    {
        get => _newUsername;
        set => SetField(ref _newUsername, value);
    }

    public string NewDisplayName
    {
        get => _newDisplayName;
        set => SetField(ref _newDisplayName, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetField(ref _newPassword, value);
    }

    public UserRole NewRole
    {
        get => _newRole;
        set => SetField(ref _newRole, value);
    }

    public IReadOnlyList<UserRole> AvailableRoles { get; } = Enum.GetValues<UserRole>();

    public bool CanManageUsers => _authorizationService.HasPermission(AppPermission.UserCreate);

    public ICommand LoadUsersCommand { get; }
    public ICommand CreateUserCommand { get; }
    public ICommand ToggleUserStatusCommand { get; }

    public UserListViewModel(
        IUserManagementService userManagementService,
        IAuthorizationService authorizationService,
        ISessionService sessionService)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        LoadUsersCommand = new Command(async () => await LoadUsersAsync(), () => !IsBusy);
        CreateUserCommand = new Command(async () => await CreateUserAsync(), () => !IsBusy && CanManageUsers);
        ToggleUserStatusCommand = new Command<User>(async (user) => await ToggleUserStatusAsync(user), (u) => !IsBusy && CanManageUsers);
    }

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    public async Task LoadUsersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var users = await _userManagementService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateUserAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _userManagementService.CreateUserAsync(NewUsername, NewPassword, NewDisplayName, NewRole);

            NewUsername = string.Empty;
            NewDisplayName = string.Empty;
            NewPassword = string.Empty;
            NewRole = UserRole.Viewer;

            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleUserStatusAsync(User? user)
    {
        if (user is null || IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _userManagementService.SetUserActiveStateAsync(user.Id, !user.IsActive);
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
