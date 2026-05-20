using System.Text.Json.Serialization;

namespace CamApp;

public sealed class MediaItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    public string FullUrl { get; set; } = "";

    public string DisplaySize
    {
        get
        {
            if (Size >= 1024 * 1024)
                return $"{Size / 1024.0 / 1024.0:0.0} MB";
            if (Size >= 1024)
                return $"{Size / 1024.0:0.0} KB";
            return $"{Size} B";
        }
    }

    public bool IsImage =>
        Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase);

    public bool IsVideo =>
        Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".mjpeg", StringComparison.OrdinalIgnoreCase) ||
        Name.EndsWith(".rawmjpeg", StringComparison.OrdinalIgnoreCase);
}
