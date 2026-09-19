using Microsoft.Extensions.Logging;
using WorkGrid.App.ViewModels;
using WorkGrid.App.ViewModels.Home;
using WorkGrid.App.ViewModels.Employees;
using WorkGrid.App.ViewModels.Assets;
using WorkGrid.App.Views;
using WorkGrid.App.Views.Home;
using WorkGrid.App.Views.Employees;
using WorkGrid.App.Views.Assets;

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

        // Root Architecture
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<EmployeeListViewModel>();
        builder.Services.AddTransient<EmployeeDetailViewModel>();
        builder.Services.AddTransient<AssetListViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();

        // Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<EmployeeListPage>();
        builder.Services.AddTransient<EmployeeDetailPage>();
        builder.Services.AddTransient<AssetListPage>();
        builder.Services.AddTransient<AssetDetailPage>();

        return builder.Build();
    }
}
