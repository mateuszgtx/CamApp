using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace CamApp;

public class MainPage : ContentPage
{
    private const int Port = 5000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(550);

    private readonly HttpClient _http = new() { Timeout = ProbeTimeout };
    private CancellationTokenSource? _scanCts;

    private readonly Entry _addressEntry;
    private readonly WebView _cameraWebView;
    private readonly Grid _overlay;
    private readonly ActivityIndicator _spinner;
    private readonly Label _statusLabel;

    private string _currentBaseUrl = "http://192.168.4.1:5000";

    public MainPage()
    {
        Title = "CamApp";
        BackgroundColor = Colors.Black;

        _addressEntry = new Entry
        {
            Placeholder = "Adres Raspberry Pi",
            Text = _currentBaseUrl,
            Keyboard = Keyboard.Url,
            FontSize = 13,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#888888"),
            BackgroundColor = Color.FromArgb("#101010")
        };

        var findButton = MakeSmallButton("Szukaj");
        findButton.Clicked += OnFindClicked;

        var openButton = MakeSmallButton("Otwórz");
        openButton.Clicked += OnOpenClicked;

        var galleryButton = MakeSmallButton("Galeria");
        galleryButton.Clicked += OnGalleryClicked;

        var topBar = new Grid
        {
            Padding = new Thickness(8, 6),
            BackgroundColor = Colors.Black,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        topBar.Add(_addressEntry, 0, 0);
        topBar.Add(findButton, 1, 0);
        topBar.Add(openButton, 2, 0);
        topBar.Add(galleryButton, 3, 0);

        _cameraWebView = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        _spinner = new ActivityIndicator
        {
            IsRunning = false,
            WidthRequest = 48,
            HeightRequest = 48,
            Color = Colors.White
        };

        _statusLabel = new Label
        {
            Text = "Szukam Raspberry Pi...",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 14,
            TextColor = Colors.White,
            Margin = new Thickness(20, 8)
        };

        var overlayPanel = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = 20,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                _spinner,
                _statusLabel
            }
        };

        _overlay = new Grid
        {
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.72),
            IsVisible = true,
            Children =
            {
                overlayPanel
            }
        };

        var mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        mainGrid.Add(topBar, 0, 0);

        var webContainer = new Grid();
        webContainer.Add(_cameraWebView);
        webContainer.Add(_overlay);

        mainGrid.Add(webContainer, 0, 1);

        Content = mainGrid;
    }

    private static Button MakeSmallButton(string text) => new()
    {
        Text = text,
        Padding = new Thickness(10, 6),
        FontSize = 13,
        BackgroundColor = Color.FromArgb("#181818"),
        TextColor = Colors.White,
        BorderColor = Color.FromArgb("#333333"),
        BorderWidth = 1,
        CornerRadius = 10,
        Margin = new Thickness(3, 0)
    };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;
        await FindAndOpenAsync();
    }

    protected override void OnDisappearing()
    {
        _scanCts?.Cancel();
        DeviceDisplay.Current.KeepScreenOn = false;
        base.OnDisappearing();
    }

    private async void OnFindClicked(object? sender, EventArgs e)
    {
        await FindAndOpenAsync();
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        OpenAddress(_addressEntry.Text);
    }

    private async void OnGalleryClicked(object? sender, EventArgs e)
    {
        var address = NormalizeBaseUrl(_addressEntry.Text);
        if (string.IsNullOrWhiteSpace(address))
            address = _currentBaseUrl;

        _currentBaseUrl = address;
        await Navigation.PushAsync(new GalleryPage(_currentBaseUrl));
    }

    private async Task FindAndOpenAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        _overlay.IsVisible = true;
        _spinner.IsRunning = true;
        _statusLabel.Text = "Szukam Raspberry Pi w sieci Wi-Fi...";

        try
        {
            foreach (var fast in new[] { "http://192.168.4.1:5000", "http://raspberrypi.local:5000" })
            {
                if (await IsPiCameraAsync(fast, token))
                {
                    OpenAddress(fast);
                    return;
                }
            }

            var candidates = BuildCandidates().Distinct().ToList();
            var found = await ScanCandidatesAsync(candidates, token);

            if (found is not null)
            {
                OpenAddress(found);
                return;
            }

            _spinner.IsRunning = false;
            _statusLabel.Text = "Nie znaleziono urządzenia. Sprawdź, czy telefon jest połączony z hotspotem Raspberry Pi.";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _spinner.IsRunning = false;
            _statusLabel.Text = "Błąd wyszukiwania: " + ex.Message;
        }
    }

    private void OpenAddress(string? address)
    {
        address = NormalizeBaseUrl(address);
        if (string.IsNullOrWhiteSpace(address))
            return;

        _currentBaseUrl = address;
        _addressEntry.Text = address;
        _cameraWebView.Source = address;
        _overlay.IsVisible = false;
        _spinner.IsRunning = false;
    }

    private static string NormalizeBaseUrl(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        address = address.Trim().TrimEnd('/');

        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            address = "http://" + address;
        }

        return address;
    }

    private async Task<string?> ScanCandidatesAsync(List<string> candidates, CancellationToken token)
    {
        var queue = new ConcurrentQueue<string>(candidates);
        using var foundCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        var workers = Enumerable.Range(0, 24).Select(_ => Task.Run(async () =>
        {
            while (!foundCts.IsCancellationRequested && queue.TryDequeue(out var url))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _statusLabel.Text = "Sprawdzam: " + url;
                });

                if (await IsPiCameraAsync(url, foundCts.Token))
                {
                    foundCts.Cancel();
                    return url;
                }
            }

            return null;
        }, foundCts.Token)).ToList();

        while (workers.Count > 0)
        {
            var finished = await Task.WhenAny(workers);
            workers.Remove(finished);

            var result = await finished;
            if (result is not null)
                return result;
        }

        return null;
    }

    private async Task<bool> IsPiCameraAsync(string baseUrl, CancellationToken token)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(ProbeTimeout);

            var url = baseUrl.TrimEnd('/') + "/api/status";
            using var response = await _http.GetAsync(url, timeout.Token);

            if (!response.IsSuccessStatusCode)
                return false;

            var text = await response.Content.ReadAsStringAsync(timeout.Token);

            return text.Contains("camera", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("ok", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("preview", StringComparison.OrdinalIgnoreCase) ||
                   IsJson(text);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsJson(string text)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> BuildCandidates()
    {
        yield return "http://192.168.4.1:5000";
        yield return "http://192.168.0.1:5000";
        yield return "http://192.168.1.1:5000";
        yield return "http://10.42.0.1:5000";
        yield return "http://10.0.0.1:5000";

        foreach (var ip in GetLocalIPv4Addresses())
        {
            var parts = ip.ToString().Split('.');
            if (parts.Length != 4)
                continue;

            var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

            yield return $"http://{prefix}1:{Port}";
            yield return $"http://{prefix}4:{Port}";
            yield return $"http://{prefix}10:{Port}";
            yield return $"http://{prefix}100:{Port}";

            for (var i = 1; i <= 254; i++)
                yield return $"http://{prefix}{i}:{Port}";
        }
    }

    private static IEnumerable<IPAddress> GetLocalIPv4Addresses()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var props = ni.GetIPProperties();

            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address))
                {
                    yield return addr.Address;
                }
            }
        }
    }
}
