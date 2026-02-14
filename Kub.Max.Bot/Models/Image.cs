using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Изображение.

public class Image
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}