## CEGŁA

`CamApp` is a .NET MAUI companion application for the CEGŁA / `pi-camera` Raspberry Pi camera project. It lets a phone or desktop client discover a camera device on the local network, connect to its web panel, browse the remote media gallery, open videos, download photos, and share photos from the device.

The app is primarily structured around Android, but the project also contains the standard .NET MAUI platform folders for iOS, Mac Catalyst and Windows.

## Features

- Camera device menu with saved camera addresses.
- Manual connection by IP address or URL.
- Local network scanning for camera devices listening on port `5000`.
- Embedded `WebView` for the remote camera web panel.
- Connection monitor that returns to the menu when the camera becomes unreachable.
- Remote gallery loaded from the camera API.
- Two-column gallery grid with image thumbnails and video badges.
- Full-screen image viewer with previous/next navigation.
- Photo download to the phone gallery on Android.
- Photo sharing through the system share sheet.
- Video opening through the system launcher.
- Cleartext HTTP support for local camera devices.
- Saved device persistence through MAUI `Preferences`.

## Project structure

```text
CamApp-master/
├── CamApp.slnx
├── CamApp/
│   ├── App.cs
│   ├── CamApp.csproj
│   ├── MainPage.cs                  # main UI, scanning, connection, gallery and viewer logic
│   ├── Models.cs                    # media item model used by the gallery API
│   ├── PhotoDownloadService.cs      # Android MediaStore download helper
│   ├── GalleryPage.cs               # placeholder; gallery is handled inside MainPage
│   ├── PhotoViewerPage.cs           # placeholder; photo viewer is handled inside MainPage
│   ├── MauiProgram.cs
│   ├── Platforms/
│   │   ├── Android/
│   │   │   ├── AndroidManifest.xml
│   │   │   ├── MainActivity.cs
│   │   │   ├── MainApplication.cs
│   │   │   └── Resources/
│   │   ├── iOS/
│   │   ├── MacCatalyst/
│   │   └── Windows/
│   ├── Properties/
│   └── Resources/
├── docs/
│   ├── ANDROID_BUILD.md
│   ├── API_COMPATIBILITY.md
│   ├── ARCHITECTURE.md
│   └── TROUBLESHOOTING.md
├── .gitattributes
└── .gitignore
```

## Requirements

### Development machine

- .NET SDK compatible with the project target framework: `net10.0`.
- .NET MAUI workload.
- Android SDK and Java tooling for Android builds.
- Visual Studio, Visual Studio Code, JetBrains Rider, or command-line `dotnet` tools.

### Camera server

The app expects a camera server compatible with the CEGŁA / `pi-camera` API, normally running at:

```text
http://<CAMERA_IP>:5000
```

The default address shown by the app is:

```text
http://192.168.4.1:5000
```

That address is useful when the Raspberry Pi camera is running as a hotspot.

## Build

Restore and build the Android target:

```bash
dotnet restore
dotnet build CamApp/CamApp.csproj -f net10.0-android -c Debug
```

Create a release Android package:

```bash
dotnet publish CamApp/CamApp.csproj -f net10.0-android -c Release
```

For more detailed Android setup and packaging notes, see [`docs/ANDROID_BUILD.md`](docs/ANDROID_BUILD.md).

## Quick start

1. Start the Raspberry Pi camera server and make sure its web panel is available on port `5000`.
2. Connect the phone to the same Wi-Fi network as the camera, or connect it to the camera hotspot.
3. Install and open `CamApp`.
4. Tap `Odśwież` to scan the network, or tap `+ Dodaj ręcznie` and enter the camera address manually.
5. Tap `Połącz` to open the camera web panel inside the app.
6. Tap `Galeria` to browse remote photos and videos.

## Typical camera addresses

The scanner checks common camera and hotspot addresses first:

```text
http://192.168.4.1:5000
http://192.168.0.1:5000
http://192.168.1.1:5000
http://10.42.0.1:5000
http://10.0.0.1:5000
```

It also scans the `/24` subnet of the current local IPv4 address.

## App screens

### Camera menu

The startup screen shows saved camera devices. From this screen the user can:

- connect to a saved camera,
- add a camera address manually,
- start or stop a network scan.

### Camera web panel

After connecting, the app loads the camera base URL in a `WebView`. The UI shown here is served by the Raspberry Pi camera application, not by `CamApp` itself.

### Gallery

The gallery reads:

```text
GET /api/photos
```

The returned media list is displayed as a two-column grid. Image files are shown as thumbnails. Video files are shown with a video badge and opened using the operating system launcher.

### Photo viewer

The photo viewer supports:

- previous/next navigation,
- download to phone,
- system sharing,
- return to the gallery.

## Supported media types

Images:

```text
.jpg
.jpeg
.png
.bmp
```

Videos:

```text
.mp4
.avi
.mjpeg
.rawmjpeg
```

## Expected camera API

`CamApp` does not implement a camera backend. It connects to an existing camera HTTP server.

The most important endpoint is:

```http
GET /api/photos
```

Expected JSON shape:

```json
[
  {
    "name": "IMG_20260101_120000000.jpg",
    "size": 123456,
    "created": "2026-01-01T12:00:00",
    "url": "/api/photos/IMG_20260101_120000000.jpg"
  }
]
```

Full compatibility details are documented in [`docs/API_COMPATIBILITY.md`](docs/API_COMPATIBILITY.md).

## Android permissions and networking

The Android manifest enables:

- `ACCESS_NETWORK_STATE`,
- `ACCESS_WIFI_STATE`,
- `INTERNET`,
- legacy `WRITE_EXTERNAL_STORAGE` up to Android API 28.

The app also allows cleartext HTTP traffic because the camera normally runs on a local `http://` address rather than `https://`.

```xml
<application
    android:usesCleartextTraffic="true"
    android:networkSecurityConfig="@xml/network_security_config" />
```

## Notes for maintainers

- The Android app label is `CEGŁA`.
- The project application ID is `com.companyname.camapp`; change it before publishing a public release.
- The current application display version is `1.0` and application version is `6`.
- `MainPage` intentionally avoids `NavigationPage`, `Shell`, `CollectionView` and `CarouselView`. Comments in the code indicate this was done to avoid Android fragment crashes related to `NavigationRootManager` / `jumpToStart`.
- `GalleryPage.cs` and `PhotoViewerPage.cs` are placeholders; the current gallery and photo viewer are implemented directly inside `MainPage.cs`.
- The UI text in the source code is currently Polish. This README is written in English for GitHub.

## Security notes

`CamApp` is designed for a trusted local network. It connects to local HTTP camera endpoints and displays their pages inside a `WebView`. Do not use it with untrusted camera URLs unless the backend is secured and trusted.

The app stores saved camera addresses locally through MAUI `Preferences`.

## Troubleshooting

Common problems and fixes are listed in [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md).
