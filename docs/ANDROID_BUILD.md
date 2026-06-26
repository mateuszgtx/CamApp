# Android build guide

This project is a .NET MAUI application. The main target in the project file is:

```text
net10.0-android
```

## Prerequisites

Install the following on the development machine:

- .NET SDK compatible with `net10.0`,
- .NET MAUI workload,
- Android SDK,
- Java tooling required by Android builds,
- an Android emulator or a physical Android device with USB debugging enabled.

Install or repair MAUI workloads:

```bash
dotnet workload install maui
```

Check installed workloads:

```bash
dotnet workload list
```

## Restore

From the repository root:

```bash
dotnet restore CamApp/CamApp.csproj
```

## Debug build

```bash
dotnet build CamApp/CamApp.csproj -f net10.0-android -c Debug
```

## Run on a connected Android device

Connect a device with USB debugging enabled, then run:

```bash
dotnet build CamApp/CamApp.csproj -f net10.0-android -t:Run
```

You can check connected devices with:

```bash
adb devices
```

## Release package

```bash
dotnet publish CamApp/CamApp.csproj -f net10.0-android -c Release
```

The generated APK/AAB location depends on the installed .NET/MAUI tooling version and build settings, but it is normally under:

```text
CamApp/bin/Release/net10.0-android/
```

or a `publish` subdirectory inside that path.

## Install an APK manually

After building an APK, install it with:

```bash
adb install -r path/to/app.apk
```

## Application identity

Current values from `CamApp.csproj`:

| Property | Value |
|---|---|
| `ApplicationTitle` | `CamApp` |
| `ApplicationId` | `com.companyname.camapp` |
| `ApplicationDisplayVersion` | `1.0` |
| `ApplicationVersion` | `6` |

The Android manifest label is:

```text
CEGŁA
```

Before publishing publicly, change `ApplicationId` from the template value to a unique reverse-DNS package name.

## Android permissions

The project requests:

```xml
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="28" />
```

`WRITE_EXTERNAL_STORAGE` is only used for older Android versions. On Android 10 and newer, photo downloads use `MediaStore`.

## Cleartext HTTP

The camera device normally uses local `http://` URLs. Android cleartext traffic is enabled in the manifest and network security config:

```xml
<base-config cleartextTrafficPermitted="true" />
```

This is required for URLs such as:

```text
http://192.168.4.1:5000
```

## Signing notes

For internal testing, a debug or default development-signed APK is usually enough.

For store/public distribution, configure release signing with a real keystore and keep the keystore private. Do not commit signing keys or passwords to Git.

## Recommended test checklist

Before releasing a build, test:

- opening the app with no saved devices,
- manual address entry without `http://`,
- manual address entry with a full URL,
- network scan on the same Wi-Fi as the camera,
- network scan when the camera is offline,
- connecting to the camera hotspot address `192.168.4.1:5000`,
- opening the WebView panel,
- opening the gallery,
- viewing images,
- opening videos,
- downloading a photo,
- sharing a photo,
- disconnecting the camera while the app is connected.
