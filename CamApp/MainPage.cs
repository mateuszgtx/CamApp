using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Maui.Controls.Shapes;

namespace CamApp;

public class MainPage : ContentPage
{
    private const int Port = 5000;
    private const string SavedDevicesPreferencesKey = "SavedCameraDevices";
    private const int ProbeTimeoutMs = 120;
    private const int ScanPauseBetweenIpsMs = 180;
    private const int ConnectTimeoutMs = 10_000;
    private const int DisconnectProbeTimeoutMs = 2_000;
    private static readonly TimeSpan ScanProgressUiInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _galleryHttp = new() { Timeout = TimeSpan.FromSeconds(20) };
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _connectionCts;
    private bool _isConnected;

    private readonly Entry _addressEntry;
    private readonly VerticalStackLayout _savedDevicesList;
    private readonly WebView _cameraWebView;
    private readonly Grid _overlay;
    private readonly ActivityIndicator _spinner;
    private readonly Label _statusLabel;
    private readonly View _connectionView;
    private readonly View _cameraView;
    private readonly Border _manualPanel;
    private readonly Button _stopScanButton;

    private readonly List<string> _savedDevices = new();
    private readonly List<MediaItem> _galleryItems = new();
    private int _photoIndex;

    private string _currentBaseUrl = "http://192.168.4.1:5000";

    public MainPage()
    {
        Title = "CamApp";
        BackgroundColor = Colors.Black;

        _addressEntry = new Entry
        {
            Placeholder = "np. 192.168.4.1:5000",
            Text = _currentBaseUrl,
            Keyboard = Keyboard.Url,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Fill,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#888888"),
            BackgroundColor = Color.FromArgb("#101010"),
            Margin = new Thickness(0, 8, 0, 0)
        };

        _savedDevicesList = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(0, 8, 0, 0)
        };

        var addManualButton = MakePrimaryButton("+ Dodaj ręcznie");
        addManualButton.Clicked += OnAddManualClicked;

        var refreshButton = MakePrimaryButton("⟳ Odśwież");
        refreshButton.Clicked += OnRefreshClicked;

        _stopScanButton = MakePrimaryButton("■ Stop");
        _stopScanButton.IsVisible = false;
        _stopScanButton.Clicked += (_, _) => _scanCts?.Cancel();

        var openButton = MakeSmallButton("Połącz");
        openButton.Clicked += OnOpenClicked;

        var saveManualButton = MakeSmallButton("Zapisz");
        saveManualButton.Clicked += OnSaveManualClicked;

        var menuTitle = new Label
        {
            Text = "Kamery",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Margin = new Thickness(2, 4, 0, 4)
        };

        var topButtons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10,
            Margin = new Thickness(0, 4, 0, 8)
        };
        topButtons.Add(addManualButton, 0, 0);
        topButtons.Add(refreshButton, 1, 0);
        topButtons.Add(_stopScanButton, 2, 0);

        var manualButtons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };
        manualButtons.Add(saveManualButton, 0, 0);
        manualButtons.Add(openButton, 1, 0);

        _manualPanel = new Border
        {
            IsVisible = false,
            Stroke = Color.FromArgb("#2E2E2E"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#161616"),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 2, 0, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = "Adres kamery",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    _addressEntry,
                    manualButtons
                }
            }
        };

        _statusLabel = new Label
        {
            Text = "",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 13,
            TextColor = Color.FromArgb("#BDBDBD"),
            Margin = new Thickness(0, 6, 0, 0)
        };

        _spinner = new ActivityIndicator
        {
            IsRunning = false,
            WidthRequest = 36,
            HeightRequest = 36,
            Color = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        var menuStack = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(18, 24),
            Children =
            {
                menuTitle,
                topButtons,
                _manualPanel,
                _savedDevicesList,
                _spinner,
                _statusLabel
            }
        };

        _connectionView = new ScrollView
        {
            BackgroundColor = Colors.Black,
            Content = menuStack
        };

        _cameraWebView = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var menuButton = MakeSmallButton("Kamery");
        menuButton.Clicked += (_, _) =>
        {
            DisconnectToMenu("");
        };

        var galleryButton = MakeSmallButton("Galeria");
        galleryButton.Clicked += OnGalleryClicked;

        var cameraTopBar = new Grid
        {
            Padding = new Thickness(8, 6),
            BackgroundColor = Colors.Black,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        cameraTopBar.Add(menuButton, 0, 0);
        cameraTopBar.Add(galleryButton, 1, 0);

        var overlayPanel = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = 20,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new ActivityIndicator
                {
                    IsRunning = false,
                    WidthRequest = 48,
                    HeightRequest = 48,
                    Color = Colors.White
                },
                new Label
                {
                    Text = "Wybierz kamerę z menu.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontSize = 14,
                    TextColor = Colors.White,
                    Margin = new Thickness(20, 8)
                }
            }
        };

        _overlay = new Grid
        {
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.72),
            IsVisible = true,
            Children = { overlayPanel }
        };

        var mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        mainGrid.Add(cameraTopBar, 0, 0);

        var webContainer = new Grid();
        webContainer.Add(_cameraWebView);
        webContainer.Add(_overlay);

        mainGrid.Add(webContainer, 0, 1);

        _cameraView = mainGrid;
        Content = _connectionView;
    }

    private static Button MakeSmallButton(string text) => new()
    {
        Text = text,
        Padding = new Thickness(10, 8),
        FontSize = 13,
        BackgroundColor = Color.FromArgb("#1F1F1F"),
        TextColor = Colors.White,
        BorderColor = Color.FromArgb("#3A3A3A"),
        BorderWidth = 1,
        CornerRadius = 12,
        Margin = new Thickness(3, 0)
    };

    private static Button MakePrimaryButton(string text) => new()
    {
        Text = text,
        Padding = new Thickness(12, 12),
        FontSize = 15,
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = Color.FromArgb("#242424"),
        TextColor = Colors.White,
        BorderColor = Color.FromArgb("#4A4A4A"),
        BorderWidth = 1,
        CornerRadius = 16,
        HorizontalOptions = LayoutOptions.Fill
    };

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;

        LoadSavedDevices();

        // Nie skanuj automatycznie przy starcie. Dzięki temu ręczne wpisanie adresu
        // i wcześniejsze presety nie są nadpisywane przez wyszukiwanie.
        _statusLabel.Text = _savedDevices.Count > 0 ? "" : "Brak urządzeń";
    }

    protected override void OnDisappearing()
    {
        _scanCts?.Cancel();
        DeviceDisplay.Current.KeepScreenOn = false;
        base.OnDisappearing();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        await ConnectToAddressAsync(_addressEntry.Text, saveDevice: true);
    }

    private void OnAddManualClicked(object? sender, EventArgs e)
    {
        _manualPanel.IsVisible = !_manualPanel.IsVisible;
        if (_manualPanel.IsVisible)
            _addressEntry.Focus();
    }

    private void OnSaveManualClicked(object? sender, EventArgs e)
    {
        var address = NormalizeBaseUrl(_addressEntry.Text);
        if (string.IsNullOrWhiteSpace(address))
            return;

        AddSavedDevice(address);
        _addressEntry.Text = address;
        _manualPanel.IsVisible = false;
        _statusLabel.Text = "Dodano";
    }

    private async void OnGalleryClicked(object? sender, EventArgs e)
    {
        var address = NormalizeBaseUrl(_addressEntry.Text);
        if (string.IsNullOrWhiteSpace(address))
            address = _currentBaseUrl;

        _currentBaseUrl = address;
        AddSavedDevice(address);
        await ShowGalleryAsync();
    }

    private async Task ShowGalleryAsync()
    {
        var status = new Label
        {
            Text = "Ładuję zdjęcia...",
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(12)
        };

        var spinner = new ActivityIndicator
        {
            IsRunning = true,
            Color = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        var list = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var backButton = MakeSmallButton("Wróć do kamery");
        backButton.Clicked += (_, _) => Content = _cameraView;

        var refreshButton = MakeSmallButton("Odśwież");
        refreshButton.Clicked += async (_, _) => await ShowGalleryAsync();

        var top = new Grid
        {
            Padding = new Thickness(8),
            BackgroundColor = Colors.Black,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        top.Add(backButton, 0, 0);
        top.Add(refreshButton, 1, 0);

        var layout = new Grid
        {
            BackgroundColor = Colors.Black,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        layout.Add(top, 0, 0);
        layout.Add(spinner, 0, 1);
        layout.Add(status, 0, 2);
        layout.Add(new ScrollView { Content = list }, 0, 3);
        Content = layout;

        try
        {
            _galleryItems.Clear();
            var url = _currentBaseUrl.TrimEnd('/') + "/api/photos";
            var items = await _galleryHttp.GetFromJsonAsync<List<MediaItem>>(url) ?? new List<MediaItem>();

            foreach (var item in items.Where(i => i.IsImage || i.IsVideo))
            {
                item.FullUrl = _currentBaseUrl.TrimEnd('/') + item.Url;
                _galleryItems.Add(item);
            }

            list.Children.Clear();
            if (_galleryItems.Count == 0)
            {
                status.Text = "Brak zdjęć.";
                return;
            }

            status.Text = "";

            var tilesGrid = BuildGalleryTilesGrid(_galleryItems);
            list.Children.Add(tilesGrid);
        }
        catch (Exception ex)
        {
            status.Text = "Nie udało się pobrać listy zdjęć: " + ex.Message;
        }
        finally
        {
            spinner.IsRunning = false;
        }
    }

    private Grid BuildGalleryTilesGrid(IReadOnlyList<MediaItem> items)
    {
        var grid = new Grid
        {
            Padding = new Thickness(6),
            RowSpacing = 12,
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        for (var i = 0; i < items.Count; i++)
        {
            if (i % 2 == 0)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tile = MakeGalleryTile(items[i]);
            grid.Add(tile, i % 2, i / 2);
        }

        return grid;
    }

    private Border MakeGalleryTile(MediaItem item)
    {
        var preview = new Grid
        {
            HeightRequest = 132,
            BackgroundColor = Color.FromArgb("#111111")
        };

        if (item.IsImage)
        {
            preview.Children.Add(new Image
            {
                Source = ImageSource.FromUri(new Uri(item.FullUrl)),
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.Black
            });
        }
        else
        {
            preview.Children.Add(new Label
            {
                Text = "🎬",
                FontSize = 42,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        var typeBadge = new Label
        {
            Text = item.IsVideo ? "WIDEO" : "ZDJĘCIE",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.62),
            Padding = new Thickness(7, 3),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(8)
        };
        preview.Children.Add(typeBadge);

        var name = new Label
        {
            Text = item.Name,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            Margin = new Thickness(8, 8, 8, 0)
        };

        var size = new Label
        {
            Text = item.DisplaySize,
            FontSize = 11,
            TextColor = Color.FromArgb("#A8A8A8"),
            LineBreakMode = LineBreakMode.TailTruncation,
            Margin = new Thickness(8, 0, 8, 8)
        };

        var content = new VerticalStackLayout
        {
            Spacing = 2,
            Children = { preview, name, size }
        };

        var tile = new Border
        {
            Stroke = Color.FromArgb("#2F2F2F"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#171717"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = content
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OpenMediaItemAsync(item);
        tile.GestureRecognizers.Add(tap);

        return tile;
    }

    private async Task OpenMediaItemAsync(MediaItem item)
    {
        if (item.IsVideo)
        {
            await Launcher.Default.OpenAsync(item.FullUrl);
            return;
        }

        var images = _galleryItems.Where(x => x.IsImage).ToList();
        _photoIndex = Math.Max(0, images.FindIndex(x => x.Name == item.Name));
        ShowPhotoViewer(images);
    }

    private void ShowPhotoViewer(List<MediaItem> images)
    {
        if (images.Count == 0)
            return;

        _photoIndex = Math.Clamp(_photoIndex, 0, images.Count - 1);
        var current = images[_photoIndex];

        var counter = new Label
        {
            Text = $"{_photoIndex + 1} / {images.Count}",
            TextColor = Colors.White,
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.55),
            Padding = new Thickness(10, 6),
            HorizontalTextAlignment = TextAlignment.Center
        };

        var image = new Image
        {
            Source = ImageSource.FromUri(new Uri(current.FullUrl)),
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var name = new Label
        {
            Text = current.Name,
            TextColor = Colors.White,
            Padding = new Thickness(10, 6),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var prevButton = MakeSmallButton("Poprzednie");
        prevButton.Clicked += (_, _) =>
        {
            _photoIndex = Math.Max(0, _photoIndex - 1);
            ShowPhotoViewer(images);
        };

        var nextButton = MakeSmallButton("Następne");
        nextButton.Clicked += (_, _) =>
        {
            _photoIndex = Math.Min(images.Count - 1, _photoIndex + 1);
            ShowPhotoViewer(images);
        };

        var downloadButton = MakeSmallButton("Download");
        downloadButton.Clicked += async (_, _) => await DownloadPhotoAsync(current);

        var shareButton = MakeSmallButton("Udostępnij");
        shareButton.Clicked += async (_, _) => await SharePhotoAsync(current);

        var backButton = MakeSmallButton("Wróć");
        backButton.Clicked += async (_, _) => await ShowGalleryAsync();

        var row1 = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        row1.Add(prevButton, 0, 0);
        row1.Add(nextButton, 1, 0);

        var row2 = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        row2.Add(downloadButton, 0, 0);
        row2.Add(shareButton, 1, 0);
        row2.Add(backButton, 2, 0);

        var buttons = new VerticalStackLayout
        {
            Spacing = 6,
            Padding = new Thickness(8),
            Children = { row1, row2 }
        };

        var layout = new Grid
        {
            BackgroundColor = Colors.Black,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        layout.Add(counter, 0, 0);
        layout.Add(name, 0, 1);
        layout.Add(image, 0, 2);
        layout.Add(buttons, 0, 3);
        Content = layout;
    }

    private async Task DownloadPhotoAsync(MediaItem item)
    {
        try
        {
            var bytes = await _galleryHttp.GetByteArrayAsync(item.FullUrl);
            await PhotoDownloadService.SaveToPhoneAsync(item.Name, bytes);
            await DisplayAlert("Download", "Zdjęcie zapisane w telefonie.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Download", "Nie udało się zapisać zdjęcia: " + ex.Message, "OK");
        }
    }

    private async Task SharePhotoAsync(MediaItem item)
    {
        try
        {
            var bytes = await _galleryHttp.GetByteArrayAsync(item.FullUrl);
            var fileName = PhotoDownloadService.CleanFileName(item.Name);
            var localPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(localPath, bytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Zdjęcie z kamery",
                File = new ShareFile(localPath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Udostępnij", "Nie udało się udostępnić zdjęcia: " + ex.Message, "OK");
        }
    }

    private async Task RefreshDevicesAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        var token = _scanCts.Token;

        Content = _connectionView;
        _spinner.IsRunning = true;
        _stopScanButton.IsVisible = true;
        _statusLabel.Text = "Skanuję port 5000... Możesz dodać ręcznie.";

        try
        {
            // Budowanie listy interfejsów i kandydatów robimy poza UI.
            // Skan jest celowo wolniejszy i spokojny, żeby nie zawieszać Androida.
            var candidates = await Task.Run(() => BuildCandidates()
                .Distinct()
                .ToList(), token);

            var lastProgressUiUpdate = DateTimeOffset.MinValue;
            var found = await Task.Run(async () =>
            {
                return await ScanAllCandidatesAsync(candidates, token, (currentIp, scanned, total) =>
                {
                    var now = DateTimeOffset.UtcNow;
                    if (now - lastProgressUiUpdate < ScanProgressUiInterval && scanned < total)
                        return;

                    lastProgressUiUpdate = now;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _statusLabel.Text = $"Skanuję {currentIp}  ({scanned}/{total}) • możesz dodać ręcznie";
                    });
                }).ConfigureAwait(false);
            }, token).ConfigureAwait(true);

            foreach (var url in found)
                AddSavedDevice(url);

            if (found.Count == 0)
            {
                _statusLabel.Text = token.IsCancellationRequested ? "Przerwano" : "Nie znaleziono";
                return;
            }

            _statusLabel.Text = found.Count == 1
                ? "Znaleziono 1 urządzenie"
                : $"Znaleziono: {found.Count}";

            _addressEntry.Text = found[0];
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Przerwano";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Błąd: " + ex.Message;
        }
        finally
        {
            _spinner.IsRunning = false;
            _stopScanButton.IsVisible = false;
        }
    }

    private async Task ConnectToAddressAsync(string? address, bool saveDevice)
    {
        address = NormalizeBaseUrl(address);
        if (string.IsNullOrWhiteSpace(address))
            return;

        _scanCts?.Cancel();
        _connectionCts?.Cancel();
        _connectionCts = new CancellationTokenSource();
        var token = _connectionCts.Token;

        Content = _connectionView;
        _spinner.IsRunning = true;
        _statusLabel.Text = $"Łączę z {GetHostFromUrl(address)}...";

        try
        {
            var canConnect = await Task.Run(() => IsPortOpen(address, token, ConnectTimeoutMs), token);
            if (!canConnect || token.IsCancellationRequested)
            {
                _statusLabel.Text = $"Nie udało się połączyć z {GetHostFromUrl(address)}";
                return;
            }

            _isConnected = true;
            _currentBaseUrl = address;
            _addressEntry.Text = address;
            _cameraWebView.Source = address;
            _overlay.IsVisible = false;
            Content = _cameraView;

            if (saveDevice)
                AddSavedDevice(address);

            StartConnectionMonitor(address, _connectionCts.Token);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Przerwano łączenie";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Nie udało się połączyć: " + ex.Message;
        }
        finally
        {
            _spinner.IsRunning = false;
        }
    }

    private void StartConnectionMonitor(string address, CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            var failedChecks = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3000, token).ConfigureAwait(false);

                    var ok = IsPortOpen(address, token, DisconnectProbeTimeoutMs);
                    failedChecks = ok ? 0 : failedChecks + 1;

                    if (failedChecks >= 2 && !token.IsCancellationRequested)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_isConnected)
                                DisconnectToMenu("Rozłączono z urządzeniem");
                        });
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    failedChecks++;
                    if (failedChecks >= 2 && !token.IsCancellationRequested)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_isConnected)
                                DisconnectToMenu("Rozłączono z urządzeniem");
                        });
                        break;
                    }
                }
            }
        }, token);
    }

    private void DisconnectToMenu(string message)
    {
        _isConnected = false;
        _connectionCts?.Cancel();
        _cameraWebView.Source = "about:blank";
        _overlay.IsVisible = true;
        Content = _connectionView;

        if (!string.IsNullOrWhiteSpace(message))
            _statusLabel.Text = message;
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

    private void LoadSavedDevices()
    {
        _savedDevices.Clear();

        try
        {
            var json = Preferences.Get(SavedDevicesPreferencesKey, "[]");
            var devices = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

            foreach (var device in devices)
            {
                var normalized = NormalizeBaseUrl(device);
                if (!string.IsNullOrWhiteSpace(normalized) && !_savedDevices.Contains(normalized))
                    _savedDevices.Add(normalized);
            }
        }
        catch
        {
            // Uszkodzony zapis presetów nie powinien blokować aplikacji.
            _savedDevices.Clear();
        }

        if (_savedDevices.Count == 0)
            _savedDevices.Add(_currentBaseUrl);

        RefreshSavedDevicesList();
    }

    private void AddSavedDevice(string address)
    {
        address = NormalizeBaseUrl(address);
        if (string.IsNullOrWhiteSpace(address))
            return;

        if (!_savedDevices.Contains(address))
            _savedDevices.Add(address);

        SaveSavedDevices();
        RefreshSavedDevicesList();
    }

    private void SaveSavedDevices()
    {
        var json = JsonSerializer.Serialize(_savedDevices);
        Preferences.Set(SavedDevicesPreferencesKey, json);
    }

    private void RefreshSavedDevicesList()
    {
        _savedDevicesList.Children.Clear();

        if (_savedDevices.Count == 0)
        {
            _savedDevicesList.Children.Add(new Label
            {
                Text = "Brak urządzeń",
                TextColor = Color.FromArgb("#8A8A8A"),
                FontSize = 14,
                Padding = new Thickness(8)
            });
            return;
        }

        foreach (var device in _savedDevices)
        {
            _savedDevicesList.Children.Add(MakeDeviceCard(device));
        }
    }

    private Border MakeDeviceCard(string address)
    {
        var icon = new Label
        {
            Text = "📷",
            FontSize = 26,
            WidthRequest = 44,
            HeightRequest = 44,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            BackgroundColor = Color.FromArgb("#262626"),
            TextColor = Colors.White
        };

        var name = new Label
        {
            Text = GetDeviceName(address),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var addr = new Label
        {
            Text = address,
            FontSize = 12,
            TextColor = Color.FromArgb("#A8A8A8"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var connectButton = MakeSmallButton("Połącz");
        connectButton.Margin = new Thickness(8, 0, 0, 0);
        connectButton.Clicked += async (_, _) =>
        {
            _addressEntry.Text = address;
            await ConnectToAddressAsync(address, saveDevice: false);
        };

        var texts = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { name, addr }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            VerticalOptions = LayoutOptions.Center
        };
        grid.Add(icon, 0, 0);
        grid.Add(texts, 1, 0);
        grid.Add(connectButton, 2, 0);

        var card = new Border
        {
            Stroke = Color.FromArgb("#2F2F2F"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#171717"),
            Padding = new Thickness(12),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = grid
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            _addressEntry.Text = address;
            await ConnectToAddressAsync(address, saveDevice: false);
        };
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private static string GetDeviceName(string address)
    {
        try
        {
            var uri = new Uri(address);
            return string.IsNullOrWhiteSpace(uri.Host) ? "Kamera" : uri.Host;
        }
        catch
        {
            return "Kamera";
        }
    }

    private async Task<List<string>> ScanAllCandidatesAsync(
        List<string> candidates,
        CancellationToken token,
        Action<string, int, int>? progress = null)
    {
        var found = new List<string>();
        var total = candidates.Count;
        var scanned = 0;

        // Najstabilniejszy wariant dla Androida: tylko jedna próba naraz + pauza między adresami.
        // Skan trwa dłużej, ale nie powinien blokować aplikacji ani ubijać procesu.
        foreach (var url in candidates)
        {
            if (token.IsCancellationRequested)
                break;

            scanned++;
            progress?.Invoke(GetHostFromUrl(url), scanned, total);

            try
            {
                if (IsPortOpen(url, token, ProbeTimeoutMs))
                    found.Add(url);
            }
            catch
            {
                // Skanowanie nigdy nie może wywalić aplikacji.
            }

            // Celowa pauza: wolniej, ale stabilnie. UI ma czas na kliknięcia i rysowanie.
            try
            {
                await Task.Delay(ScanPauseBetweenIpsMs, token).ConfigureAwait(false);
            }
            catch
            {
                break;
            }
        }

        return found
            .Select(NormalizeBaseUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct()
            .OrderBy(url => url)
            .ToList();
    }

    private static string GetHostFromUrl(string url)
    {
        try
        {
            return new Uri(NormalizeBaseUrl(url)).Host;
        }
        catch
        {
            return url;
        }
    }

    private bool IsPortOpen(string baseUrl, CancellationToken token, int timeoutMs)
    {
        try
        {
            if (token.IsCancellationRequested)
                return false;

            var uri = new Uri(NormalizeBaseUrl(baseUrl));
            var port = uri.IsDefaultPort ? Port : uri.Port;

#if ANDROID
            // Android: używamy natywnego, synchronicznego socketu z timeoutem.
            // To jest stabilniejsze niż TcpClient.ConnectAsync + anulowanie, bo nie zostawia
            // w tle niedokończonych połączeń, które potrafiły ubijać aplikację bez błędu.
            Java.Net.Socket? socket = null;
            try
            {
                socket = new Java.Net.Socket();
                socket.Connect(new Java.Net.InetSocketAddress(uri.Host, port), timeoutMs);
                return socket.IsConnected;
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    socket?.Close();
                }
                catch
                {
                    // Ignorujemy błędy zamykania socketu.
                }
            }
#else
            using var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var asyncResult = socket.BeginConnect(uri.Host, port, null, null);
            var success = asyncResult.AsyncWaitHandle.WaitOne(timeoutMs);
            if (!success || token.IsCancellationRequested)
                return false;

            socket.EndConnect(asyncResult);
            return socket.Connected;
#endif
        }
        catch
        {
            return false;
        }
    }

    private static bool IsJsonBody(string httpResponse)
    {
        var bodyIndex = httpResponse.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        var body = bodyIndex >= 0 ? httpResponse[(bodyIndex + 4)..] : httpResponse;
        return IsJson(body);
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
        // Stałe, najczęstsze adresy bez DNS, żeby odświeżanie nie wisiało na nazwach typu .local.
        yield return "http://192.168.4.1:5000";
        yield return "http://192.168.0.1:5000";
        yield return "http://192.168.1.1:5000";
        yield return "http://10.42.0.1:5000";
        yield return "http://10.0.0.1:5000";

        foreach (var ip in GetLocalIPv4Addresses())
        {
            var parts = ip.Split('.');
            if (parts.Length != 4)
                continue;

            var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

            // Pełny zakres /24, ale sprawdzany spokojnie w tle.
            for (var i = 1; i <= 254; i++)
                yield return $"http://{prefix}{i}:{Port}";
        }
    }

    private static IEnumerable<string> GetLocalIPv4Addresses()
    {
        var addresses = new List<string>();

#if ANDROID
        // Lżejsze na Androidzie niż System.Net.NetworkInformation.
        // Nie ładuje dodatkowego System.Net.NetworkInformation.dll podczas kliknięcia Odśwież.
        try
        {
            var wifi = Android.App.Application.Context.GetSystemService(Android.Content.Context.WifiService) as Android.Net.Wifi.WifiManager;
            var ipInt = wifi?.ConnectionInfo?.IpAddress ?? 0;
            if (ipInt != 0)
            {
                var ip = $"{ipInt & 0xff}.{(ipInt >> 8) & 0xff}.{(ipInt >> 16) & 0xff}.{(ipInt >> 24) & 0xff}";
                if (!ip.StartsWith("127.", StringComparison.Ordinal) && ip != "0.0.0.0")
                    addresses.Add(ip);
            }
        }
        catch
        {
            // Jeśli system nie odda IP Wi-Fi, zostają stałe najczęstsze adresy powyżej.
        }
#else
        try
        {
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    addresses.Add(ip.ToString());
            }
        }
        catch
        {
            // Jeśli system nie odda IP, zostają stałe najczęstsze adresy powyżej.
        }
#endif

        return addresses;
    }
}
