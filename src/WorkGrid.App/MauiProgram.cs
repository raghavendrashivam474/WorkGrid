using Microsoft.Extensions.Logging;
using WorkGrid.App.ViewModels;
using WorkGrid.App.Views;

namespace WorkGrid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Views
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
