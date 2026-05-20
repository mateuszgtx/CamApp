using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CamApp;

public class GalleryPage : ContentPage
{
    private readonly string _baseUrl;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly ObservableCollection<MediaItem> _items = new();

    private readonly CollectionView _collection;
    private readonly ActivityIndicator _spinner;
    private readonly Label _status;

    public GalleryPage(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        Title = "Galeria";
        BackgroundColor = Colors.Black;

        _spinner = new ActivityIndicator
        {
            IsRunning = false,
            Color = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        _status = new Label
        {
            Text = "",
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(12)
        };

        var refreshButton = new Button
        {
            Text = "Odśwież",
            BackgroundColor = Color.FromArgb("#181818"),
            TextColor = Colors.White,
            BorderColor = Color.FromArgb("#333333"),
            BorderWidth = 1,
            CornerRadius = 12,
            Margin = new Thickness(8, 8, 8, 4)
        };
        refreshButton.Clicked += async (_, _) => await LoadAsync();

        _collection = new CollectionView
        {
            ItemsSource = _items,
            SelectionMode = SelectionMode.Single,
            ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
            {
                HorizontalItemSpacing = 8,
                VerticalItemSpacing = 8
            },
            Margin = new Thickness(8),
            ItemTemplate = new DataTemplate(() =>
            {
                var image = new Image
                {
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 150,
                    BackgroundColor = Colors.Black
                };
                image.SetBinding(Image.SourceProperty, nameof(MediaItem.FullUrl));

                var name = new Label
                {
                    TextColor = Colors.White,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    Margin = new Thickness(8, 6, 8, 0)
                };
                name.SetBinding(Label.TextProperty, nameof(MediaItem.Name));

                var size = new Label
                {
                    TextColor = Color.FromArgb("#aaaaaa"),
                    FontSize = 11,
                    Margin = new Thickness(8, 0, 8, 8)
                };
                size.SetBinding(Label.TextProperty, nameof(MediaItem.DisplaySize));

                var frame = new Border
                {
                    Stroke = Color.FromArgb("#252525"),
                    StrokeThickness = 1,
                    BackgroundColor = Color.FromArgb("#111111"),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 0,
                        Children = { image, name, size }
                    }
                };

                return frame;
            })
        };

        _collection.SelectionChanged += OnSelectionChanged;

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        grid.Add(refreshButton, 0, 0);
        grid.Add(_spinner, 0, 1);
        grid.Add(_status, 0, 2);
        grid.Add(_collection, 0, 3);

        Content = grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _spinner.IsRunning = true;
        _status.Text = "";

        try
        {
            var url = _baseUrl + "/api/photos";
            var items = await _http.GetFromJsonAsync<List<MediaItem>>(url) ?? new List<MediaItem>();

            _items.Clear();

            foreach (var item in items.Where(i => i.IsImage || i.IsVideo))
            {
                item.FullUrl = _baseUrl + item.Url;
                _items.Add(item);
            }

            if (_items.Count == 0)
                _status.Text = "Brak zdjęć.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Galeria", "Nie udało się pobrać zdjęć: " + ex.Message, "OK");
        }
        finally
        {
            _spinner.IsRunning = false;
        }
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as MediaItem;
        _collection.SelectedItem = null;

        if (item is null)
            return;

        if (item.IsVideo)
        {
            await Launcher.Default.OpenAsync(item.FullUrl);
            return;
        }

        var images = _items.Where(x => x.IsImage).ToList();
        var index = images.FindIndex(x => x.Name == item.Name);
        await Navigation.PushAsync(new PhotoViewerPage(images, Math.Max(0, index)));
    }
}
