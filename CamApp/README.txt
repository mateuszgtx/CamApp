CamApp - prosta aplikacja MAUI bez XAML

Ta wersja NIE używa MainPage.xaml ani App.xaml.
Cały interfejs jest tworzony w C#, więc nie powinno być błędów:
- InitializeComponent does not exist
- AddressEntry does not exist
- CameraWebView does not exist
- Overlay does not exist

Budowanie:
dotnet restore
dotnet build -f net10.0-android

APK:
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk

Jeśli używasz .NET 8, zmień w CamApp.csproj net10.0-android na net8.0-android
i Microsoft.Extensions.Logging.Debug z 10.0.0 na 8.0.1.
