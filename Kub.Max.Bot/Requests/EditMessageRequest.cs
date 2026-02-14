using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на редактирование сообщения.

public class EditMessageRequest
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<AttachmentRequest>? Attachments { get; set; }

    [JsonPropertyName("link")]
    public NewMessageLink? Link { get; set; }

    [JsonPropertyName("notify")]
    public bool? Notify { get; set; } = true;

    [JsonPropertyName("format")]
    public string? Format { get; set; }
}