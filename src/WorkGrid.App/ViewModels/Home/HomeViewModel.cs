using WorkGrid.App.ViewModels.Base;

namespace WorkGrid.App.ViewModels.Home;

public sealed class HomeViewModel : ViewModelBase
{
    private string _title = "WorkGrid Dashboard";
    private string _welcomeMessage = "Welcome to your local-first Asset Tracker.";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }
}
