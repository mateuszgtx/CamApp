CamApp - pełna wersja z galerią i ikoną PNG

Ta wersja jest BEZ XAML.
Nie używa:
- App.xaml
- App.xaml.cs
- MainPage.xaml
- MainPage.xaml.cs

Pliki w projekcie:
- CamApp.csproj
- MauiProgram.cs
- App.cs
- MainPage.cs
- GalleryPage.cs
- PhotoViewerPage.cs
- Models.cs
- Platforms/Android/MainApplication.cs
- Platforms/Android/MainActivity.cs
- Platforms/Android/AndroidManifest.xml
- Platforms/Android/Resources/xml/network_security_config.xml
- Resources/AppIcon/appicon.png

Galeria:
- Przycisk "Galeria" w głównym ekranie.
- Pobiera listę z /api/photos.
- Kliknięcie zdjęcia otwiera pełny podgląd.
- Można przewijać zdjęcia palcem w lewo/prawo.
- Wideo otwiera się w zewnętrznym odtwarzaczu.

Ikona:
- Ikona jest plikiem PNG:
  Resources/AppIcon/appicon.png
- W csproj:
  <MauiIcon Include="Resources\AppIcon\appicon.png" />

Build:
dotnet restore
dotnet build -f net10.0-android

APK:
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk

Jeśli używasz .NET 8:
- zmień net10.0-android na net8.0-android
- zmień Microsoft.Extensions.Logging.Debug 10.0.0 na 8.0.1

Po zmianie ikony:
- dotnet clean
- usuń bin i obj, jeśli dalej pokazuje starą ikonę
- build/publish od nowa
