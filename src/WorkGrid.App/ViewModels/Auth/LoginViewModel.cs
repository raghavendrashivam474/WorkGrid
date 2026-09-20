using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WorkGrid.Domain.Contracts;

namespace WorkGrid.App.ViewModels.Auth;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authService;
    private readonly ISessionService _sessionService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _displayName = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isFirstRunSetup;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetField(ref _confirmPassword, value);
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

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public bool IsFirstRunSetup
    {
        get => _isFirstRunSetup;
        set
        {
            if (SetField(ref _isFirstRunSetup, value))
            {
                OnPropertyChanged(nameof(IsNormalLogin));
            }
        }
    }

    public bool IsNormalLogin => !_isFirstRunSetup;

    public ICommand LoginCommand { get; }
    public ICommand SetupAdminCommand { get; }

    public LoginViewModel(
        IAuthenticationService authService,
        ISessionService sessionService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        LoginCommand = new Command(async () => await ExecuteLoginAsync(), () => !IsBusy);
        SetupAdminCommand = new Command(async () => await ExecuteSetupAdminAsync(), () => !IsBusy);
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var hasUsers = await _authService.HasAnyUsersAsync();
            IsFirstRunSetup = !hasUsers;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Initialization error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteLoginAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Username, Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                return;
            }

            // Clear inputs for security
            Password = string.Empty;
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSetupAdminAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Username and display name are required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters long.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _authService.RegisterInitialAdminAsync(Username, Password, DisplayName);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Setup failed.";
                return;
            }

            // Clear inputs
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Setup error: {ex.Message}";
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
