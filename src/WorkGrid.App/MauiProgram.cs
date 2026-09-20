using WorkGrid.App.ViewModels.Assignments;
using WorkGrid.App.Views.Assignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkGrid.App.ViewModels;
using WorkGrid.App.ViewModels.Home;
using WorkGrid.App.ViewModels.Employees;
using WorkGrid.App.ViewModels.Assets;
using WorkGrid.App.ViewModels.Auth;
using WorkGrid.App.ViewModels.Users;
using WorkGrid.App.Views;
using WorkGrid.App.Views.Home;
using WorkGrid.App.Views.Employees;
using WorkGrid.App.Views.Assets;
using WorkGrid.App.Views.Auth;
using WorkGrid.App.Views.Users;
using WorkGrid.Infrastructure.DependencyInjection;
using WorkGrid.Infrastructure.Persistence;

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

        // Infrastructure Persistence (Local SQLite)
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "workgrid.db3");
        builder.Services.AddInfrastructure(dbPath);

        // Root Navigation
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<EmployeeListViewModel>();
        builder.Services.AddTransient<EmployeeDetailViewModel>();
        builder.Services.AddTransient<AssetListViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();
        builder.Services.AddTransient<AssignmentListViewModel>();
        builder.Services.AddTransient<AssignmentDetailViewModel>();
        builder.Services.AddTransient<UserListViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<EmployeeListPage>();
        builder.Services.AddTransient<EmployeeDetailPage>();
        builder.Services.AddTransient<AssetListPage>();
        builder.Services.AddTransient<AssetDetailPage>();
        builder.Services.AddTransient<AssignmentListPage>();
        builder.Services.AddTransient<AssignmentDetailPage>();
        builder.Services.AddTransient<UserListPage>();

        var app = builder.Build();

        // Ensure database is migrated at startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkGridDbContext>();
            dbContext.Database.Migrate();
        }

        return app;
    }
}
