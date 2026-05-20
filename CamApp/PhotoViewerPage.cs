namespace CamApp;

public class PhotoViewerPage : ContentPage
{
    private readonly List<MediaItem> _items;
    private readonly CarouselView _carousel;
    private readonly Label _counter;

    public PhotoViewerPage(List<MediaItem> items, int startIndex)
    {
        _items = items;
        Title = "Zdjęcie";
        BackgroundColor = Colors.Black;

        _counter = new Label
        {
            TextColor = Colors.White,
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.55),
            Padding = new Thickness(10, 6),
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center
        };

        _carousel = new CarouselView
        {
            ItemsSource = _items,
            Position = Math.Clamp(startIndex, 0, Math.Max(0, _items.Count - 1)),
            Loop = false,
            ItemTemplate = new DataTemplate(() =>
            {
                var image = new Image
                {
                    Aspect = Aspect.AspectFit,
                    BackgroundColor = Colors.Black
                };
                image.SetBinding(Image.SourceProperty, nameof(MediaItem.FullUrl));

                var name = new Label
                {
                    TextColor = Colors.White,
                    FontSize = 12,
                    BackgroundColor = Color.FromRgba(0, 0, 0, 0.55),
                    Padding = new Thickness(10, 6),
                    LineBreakMode = LineBreakMode.TailTruncation,
                    VerticalOptions = LayoutOptions.End
                };
                name.SetBinding(Label.TextProperty, nameof(MediaItem.Name));

                var grid = new Grid();
                grid.Add(image);
                grid.Add(name);
                return grid;
            })
        };

        _carousel.PositionChanged += (_, e) => UpdateCounter(e.CurrentPosition);

        var closeButton = new Button
        {
            Text = "Zamknij",
            BackgroundColor = Color.FromArgb("#181818"),
            TextColor = Colors.White,
            BorderColor = Color.FromArgb("#333333"),
            BorderWidth = 1,
            CornerRadius = 12,
            Margin = new Thickness(8)
        };
        closeButton.Clicked += async (_, _) => await Navigation.PopAsync();

        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        layout.Add(_counter, 0, 0);
        layout.Add(_carousel, 0, 1);
        layout.Add(closeButton, 0, 2);

        Content = layout;
        UpdateCounter(_carousel.Position);
    }

    private void UpdateCounter(int position)
    {
        _counter.Text = _items.Count == 0
            ? "Brak zdjęć"
            : $"{position + 1} / {_items.Count}";
    }
}
