# Troubleshooting

## The app does not find the camera

Check that the phone and the Raspberry Pi camera are on the same network.

Try entering the camera address manually:

```text
http://192.168.4.1:5000
```

or:

```text
http://<RASPBERRY_PI_IP>:5000
```

Also check that the camera server is actually running and listening on port `5000`.

## The scanner is slow

The scanner checks port `5000` one address at a time with a pause between attempts. This is intentional. The code comments explain that this slower approach is more stable on Android than running many socket connection attempts in parallel.

## The app connects but the WebView is blank

Possible causes:

| Cause | Fix |
|---|---|
| Wrong URL | Open the same URL in a browser on the phone |
| Camera web panel is not running | Restart the Raspberry Pi camera service |
| Phone is on a different network | Connect to the same Wi-Fi or camera hotspot |
| Backend only binds to localhost | Start the camera server on `0.0.0.0`, not only `127.0.0.1` |

## The gallery does not load

The app expects:

```http
GET /api/photos
```

The endpoint must return a JSON array with objects containing at least:

```json
{
  "name": "IMG_20260101_120000000.jpg",
  "size": 123456,
  "created": "2026-01-01T12:00:00",
  "url": "/api/photos/IMG_20260101_120000000.jpg"
}
```

Make sure `url` starts with `/`, because the app appends it to the base URL.

## Images do not open

Check that the camera server serves the image URL returned in `url`.

Supported image extensions are:

```text
.jpg
.jpeg
.png
.bmp
```

## Videos do not open

Videos are opened with the operating system launcher. The camera server must return a video file that Android can open with an installed app.

Supported video extensions recognized by the gallery are:

```text
.mp4
.avi
.mjpeg
.rawmjpeg
```

For best Android compatibility, use `.mp4`.

## Download does not save to the phone gallery

On Android 10 and newer, the app saves photos through `MediaStore` into:

```text
Pictures/CamApp
```

On older Android versions, it uses the public Pictures directory and legacy storage permissions.

Check:

- the file is an image,
- the camera URL is reachable,
- the Android device has enough storage,
- the image download completes successfully.

## Sharing fails

Sharing first downloads the file into the app cache directory, then opens the system share sheet. If it fails, check that:

- the image URL is reachable,
- the camera server returns valid image bytes,
- at least one app that can receive shared images is installed.

## App returns to the camera menu automatically

The app checks the camera connection every three seconds. After two failed checks, it assumes the camera is disconnected and returns to the menu.

This can happen when:

- the camera server is restarted,
- the phone switches Wi-Fi networks,
- the Raspberry Pi hotspot stops,
- the phone sleeps or loses network connectivity.

## Android build fails

Check installed workloads:

```bash
dotnet workload list
```

Install MAUI if needed:

```bash
dotnet workload install maui
```

Then restore and build again:

```bash
dotnet restore CamApp/CamApp.csproj
dotnet build CamApp/CamApp.csproj -f net10.0-android -c Debug
```

## Notes about UI language

The current UI strings in the source code are Polish. The app buttons include labels such as:

- `Kamery`,
- `Odśwież`,
- `Połącz`,
- `Galeria`,
- `Wróć`,
- `Udostępnij`.

For a fully English app release, these strings should be moved to resource files and localized.
