using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Формат сообщения.

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageFormat
{
    [JsonPropertyName("markdown")]
    Markdown,

    [JsonPropertyName("html")]
    Html,

    [JsonPropertyName("plain")]
    Plain
}