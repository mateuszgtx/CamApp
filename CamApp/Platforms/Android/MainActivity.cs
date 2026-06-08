using Android.App;
using Android.Content.PM;
using Android.OS;

namespace CamApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Nie odtwarzaj starego stanu fragmentów Androida.
        // W MAUI zdarza się crash typu:
        // "No view found for id ... jumpToStart for fragment NavigationRootManager..."
        // po przebudowie UI / aktualizacji aplikacji w debugowaniu.
        base.OnCreate(null);
    }
}
