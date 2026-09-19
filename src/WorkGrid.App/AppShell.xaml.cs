using WorkGrid.App.Views.Employees;
using WorkGrid.App.Views.Assets;

namespace WorkGrid.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Detail pages are navigated to, not shown in flyout
        Routing.RegisterRoute("employee-detail", typeof(EmployeeDetailPage));
        Routing.RegisterRoute("asset-detail", typeof(AssetDetailPage));
    }
}
