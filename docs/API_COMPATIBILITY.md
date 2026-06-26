# API compatibility

`CamApp` is a client application. It does not host its own API. It expects a camera HTTP server, such as the CEGŁA / `pi-camera` Raspberry Pi application, to be available on the local network.

The default port is:

```text
5000
```

The default camera URL used by the app is:

```text
http://192.168.4.1:5000
```

## Connection model

When the user connects to a camera, `CamApp` first checks whether the TCP port is reachable. If the port is open, the app loads the camera base URL in a MAUI `WebView`.

The connection check is intentionally simple:

- it opens a TCP connection to the host and port,
- it does not require HTTPS,
- it does not authenticate,
- it does not validate a specific API response before loading the page.

## Web panel

The camera web panel is loaded from the base URL:

```http
GET /
```

The actual page is served by the Raspberry Pi camera application. `CamApp` only embeds it in a `WebView`.

## Gallery endpoint

The gallery screen calls:

```http
GET /api/photos
```

Expected response:

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

The response is deserialized into `MediaItem` objects.

| JSON field | Type | Description |
|---|---|---|
| `name` | string | File name shown in the gallery and used for downloads |
| `size` | number | File size in bytes |
| `created` | string/date | Creation timestamp |
| `url` | string | Path to the downloadable media file |

## Important URL behavior

The app builds full media URLs with:

```csharp
item.FullUrl = _currentBaseUrl.TrimEnd('/') + item.Url;
```

Because of this, the `url` field should normally start with `/`:

```json
"url": "/api/photos/example.jpg"
```

If the backend returns `"api/photos/example.jpg"` without the leading slash, the final URL may be malformed.

## Supported media extensions

Images displayed in the gallery and photo viewer:

```text
.jpg
.jpeg
.png
.bmp
```

Videos opened with the system launcher:

```text
.mp4
.avi
.mjpeg
.rawmjpeg
```

## Photo download and share flow

When the user taps `Download` or `Share`, the app downloads the selected image from `item.FullUrl`.

Download behavior:

- Android API 29 and newer: saves through `MediaStore` into `Pictures/CamApp`.
- Older Android versions: writes to the public `Pictures/CamApp` directory and registers the file in `MediaStore`.
- Other platforms: writes to the MAUI app data directory.

Share behavior:

- downloads the image into the MAUI cache directory,
- opens the system share sheet with the temporary file.

## Camera backend checklist

For best compatibility with `CamApp`, the camera server should:

- listen on port `5000`, or use a URL entered manually by the user,
- serve the web panel at `/`,
- expose `GET /api/photos`,
- return media items with `name`, `size`, `created` and `url`,
- return `url` values beginning with `/`,
- serve media files with correct content types,
- allow local HTTP access from phones on the same network.
