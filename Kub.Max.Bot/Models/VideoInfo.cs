using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Информация о видео.

public class VideoInfo
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("urls")]
    public VideoUrls? Urls { get; set; }

    [JsonPropertyName("thumbnail")]
    public ImagePayload? Thumbnail { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}


/// URL-адреса видео для разных качеств.

public class VideoUrls
{
    [JsonPropertyName("low")]
    public string? Low { get; set; }

    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("high")]
    public string? High { get; set; }
}