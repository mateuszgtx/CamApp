# PiCameraMobile

Prosta aplikacja .NET MAUI na Androida, która:

1. automatycznie szuka Raspberry Pi z API kamery,
2. sprawdza endpoint `/api/status`,
3. otwiera panel webowy z Raspberry Pi w `WebView`.

Domyślny szybki adres to:

```text
http://192.168.4.1:5000
```

## Wymagania

- .NET SDK z workloadem MAUI Android.
- Telefon podłączony do hotspotu Raspberry Pi.
- Program kamery na Raspberry Pi uruchomiony z API:

```bash
dotnet run -- --fb=/dev/fb0 --width=480 --height=320 --fps=20 --touch=/dev/input/event4 --invert-y=true --invert-x=false --gpio-pin=-1 --look=LOW32 --api=true --api-url=http://0.0.0.0:5000
```

## Build APK

```bash
dotnet workload install maui-android
dotnet restore
dotnet build -f net8.0-android -c Release
```

APK/AAB znajdziesz w katalogu `bin/Release/net8.0-android/`.

## Jak działa wyszukiwanie

Aplikacja najpierw sprawdza:

- `http://192.168.4.1:5000`
- `http://raspberrypi.local:5000`

Potem skanuje podsieć telefonu, np. `192.168.4.1-254:5000` i szuka odpowiedzi z `/api/status`.

## HTTP na Androidzie

Projekt ma już ustawione:

- `android:usesCleartextTraffic="true"`
- `network_security_config.xml`
- uprawnienia `INTERNET`, `ACCESS_NETWORK_STATE`, `ACCESS_WIFI_STATE`

Dzięki temu lokalny adres `http://...` działa w WebView.
