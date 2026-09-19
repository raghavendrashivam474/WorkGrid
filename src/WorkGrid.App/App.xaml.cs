using WorkGrid.App.Views;

namespace WorkGrid.App;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();

        MainPage = mainPage;
    }
}
