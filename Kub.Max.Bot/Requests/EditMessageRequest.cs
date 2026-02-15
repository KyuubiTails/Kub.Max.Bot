using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;

/// <summary>
/// Запрос на редактирование сообщения.
/// </summary>
public class EditMessageRequest
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }

    [JsonPropertyName("link")]
    public NewMessageLink? Link { get; set; }

    [JsonPropertyName("notify")]
    public bool? Notify { get; set; } = true;

    [JsonPropertyName("format")]
    public string? Format { get; set; }
}