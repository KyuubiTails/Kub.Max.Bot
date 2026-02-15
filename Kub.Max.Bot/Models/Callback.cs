using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Callback от inline-кнопки.

public class Callback
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("callback_id")]
    public string CallbackId { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    [JsonPropertyName("mid")]
    public string? MessageId { get; set; }

    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }
}