using System.Collections.ObjectModel;
using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;
using WorkGrid.Domain.Contracts;
using WorkGrid.Domain.Entities;
using WorkGrid.Domain.Enums;
using WorkGrid.Domain.Exceptions;

namespace WorkGrid.App.ViewModels.Users;

public sealed class UserListViewModel : ViewModelBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISessionService _sessionService;

    private bool _isBusy;
    private bool _isEmpty;
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private string _newUsername = string.Empty;
    private string _newDisplayName = string.Empty;
    private string _newPassword = string.Empty;
    private UserRole _newRole = UserRole.Viewer;

    public ObservableCollection<User> Users { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((Command)LoadUsersCommand).ChangeCanExecute();
                ((Command)CreateUserCommand).ChangeCanExecute();
                ((Command)ToggleUserStatusCommand).ChangeCanExecute();
            }
        }
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

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

    public string NewUsername
    {
        get => _newUsername;
        set => SetProperty(ref _newUsername, value);
    }

    public string NewDisplayName
    {
        get => _newDisplayName;
        set => SetProperty(ref _newDisplayName, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public UserRole NewRole
    {
        get => _newRole;
        set => SetProperty(ref _newRole, value);
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
            StatusMessage = "Loading users...";

            var users = await _userManagementService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            IsEmpty = Users.Count == 0;
            StatusMessage = IsEmpty ? "No registered users. Use the form above to register one." : string.Empty;
        }
        catch (AuthorizationException authEx)
        {
            ErrorMessage = authEx.Message;
            IsEmpty = true;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load users: {ex.Message}";
            IsEmpty = true;
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateUserAsync()
    {
        if (IsBusy || !CanManageUsers) return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewUsername))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewDisplayName))
        {
            ErrorMessage = "Display Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters long.";
            return;
        }

        try
        {
            IsBusy = true;

            await _userManagementService.CreateUserAsync(NewUsername.Trim(), NewPassword, NewDisplayName.Trim(), NewRole);

            NewUsername = string.Empty;
            NewDisplayName = string.Empty;
            NewPassword = string.Empty;
            NewRole = UserRole.Viewer;

            await LoadUsersAsync();
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (AuthorizationException authEx)
        {
            ErrorMessage = authEx.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create user: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleUserStatusAsync(User? user)
    {
        if (user is null || IsBusy || !CanManageUsers) return;

        if (user.IsActive && Application.Current?.MainPage != null)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Deactivation",
                $"Are you sure you want to deactivate user '{user.DisplayName}' (@{user.Username})?",
                "Yes, Deactivate",
                "Cancel");

            if (!confirm) return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _userManagementService.SetUserActiveStateAsync(user.Id, !user.IsActive);
            await LoadUsersAsync();
        }
        catch (DomainValidationException dex)
        {
            ErrorMessage = dex.Message;
        }
        catch (AuthorizationException authEx)
        {
            ErrorMessage = authEx.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update user status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}