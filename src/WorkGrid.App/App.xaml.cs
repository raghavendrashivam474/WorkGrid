using WorkGrid.Domain.Contracts;

namespace WorkGrid.App;

public partial class App : Application
{
    private readonly ISessionService _sessionService;

    public App(AppShell appShell, ISessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        MainPage = appShell;
    }

    protected override async void OnStart()
    {
        base.OnStart();

        if (!_sessionService.IsAuthenticated)
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
