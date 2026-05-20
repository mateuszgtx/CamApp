namespace CamApp;

public class App : Application
{
    public App()
    {
        MainPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = Colors.Black,
            BarTextColor = Colors.White
        };
    }
}
