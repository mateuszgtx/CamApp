namespace CamApp;

public class App : Application
{
    public App()
    {
        // Bez NavigationPage/Shell, żeby Android nie tworzył dodatkowych fragmentów
        // typu NavigationRootManager. To usuwa crash z jumpToStart.
        MainPage = new MainPage();
    }
}
