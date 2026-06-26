# Architecture

`CamApp` is a compact .NET MAUI client for the CEGŁA / `pi-camera` device. Most of the application state and UI are handled in `MainPage.cs`.

## Main components

| Component | Responsibility |
|---|---|
| `App.cs` | Starts the application and sets `MainPage` directly as the root page |
| `MauiProgram.cs` | Configures the MAUI app and debug logging |
| `MainPage.cs` | Builds the UI, scans for cameras, connects to the web panel, displays the gallery and handles the photo viewer |
| `Models.cs` | Defines `MediaItem`, the gallery item model returned by the camera API |
| `PhotoDownloadService.cs` | Saves downloaded photos to Android `MediaStore` or app storage on other platforms |
| `MainActivity.cs` | Android entry activity; starts MAUI with a fresh state |
| `AndroidManifest.xml` | Declares network permissions and cleartext HTTP support |

## Navigation design

The app does not use `Shell` or `NavigationPage`. It swaps the root `Content` of `MainPage` between manually built views:

- connection menu,
- camera web panel,
- gallery,
- photo viewer.

The source comments explain that this avoids Android fragment crashes related to `NavigationRootManager` and `jumpToStart`.

`GalleryPage.cs` and `PhotoViewerPage.cs` are intentionally empty placeholders. The gallery and viewer are implemented directly in `MainPage.cs`.

## Runtime flow

### 1. Startup

`App` creates `MainPage`. `MainPage` builds all UI elements in code and starts on the connection menu.

When the page appears:

- the screen is kept awake,
- saved camera addresses are loaded from MAUI `Preferences`,
- the default address `http://192.168.4.1:5000` is added if no saved devices exist.

### 2. Device discovery

The user can tap `Odśwież` to scan for cameras.

Discovery uses two sources of candidate URLs:

1. common camera/hotspot addresses,
2. every IP in the `/24` subnet of the current local IPv4 address.

The scan checks port `5000` sequentially. This is slower than parallel scanning, but the comments note that it is intentionally stable for Android and avoids overloading the app with many pending socket connections.

### 3. Manual connection

The user can tap `+ Dodaj ręcznie`, enter an address, and save it.

Address normalization adds `http://` if no scheme is provided and removes a trailing slash.

### 4. Connecting to a camera

Before connecting, the app checks whether the target port is open.

If the check succeeds:

- `_currentBaseUrl` is updated,
- the URL is loaded into the `WebView`,
- the camera view is shown,
- the address is optionally saved,
- a connection monitor starts in the background.

### 5. Connection monitor

Every three seconds, the app checks whether the camera port is still reachable. After two failed checks, it returns to the camera menu and shows a disconnected message.

### 6. Gallery

The gallery calls:

```http
GET /api/photos
```

The result is parsed as a list of `MediaItem` objects. Images and videos are filtered by extension and shown in a two-column grid.

### 7. Media handling

Image items open in the internal photo viewer. Video items are opened with:

```csharp
Launcher.Default.OpenAsync(item.FullUrl)
```

Photo downloads and sharing are handled by downloading bytes from the camera server.

## Android-specific implementation

### Port checks

On Android, port checks use `Java.Net.Socket` with a fixed timeout. The source comments explain that this was chosen over `TcpClient.ConnectAsync` because it is more stable on Android and avoids leftover background connections.

### Local IP detection

On Android, the app reads the current Wi-Fi IP address through `WifiManager`. On other platforms, it uses `Dns.GetHostAddresses`.

### Photo saving

On Android API 29 and newer, photos are saved through `MediaStore` with `IS_PENDING` handling. Files are placed under:

```text
Pictures/CamApp
```

On older Android versions, the app writes to the public Pictures folder and inserts the file into `MediaStore` manually.

## Data persistence

Saved camera devices are stored with MAUI `Preferences` under the key:

```text
SavedCameraDevices
```

The value is a JSON array of normalized camera URLs.

## Networking assumptions

`CamApp` assumes the camera is reachable over local HTTP. The Android manifest enables cleartext traffic globally through:

```xml
android:usesCleartextTraffic="true"
android:networkSecurityConfig="@xml/network_security_config"
```

This is convenient for a local Raspberry Pi camera, but it should be treated as a trusted-network design.
