using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Тело нового сообщения.

public class NewMessageBody
{
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


/// Ссылка на сообщение при ответе/пересылке.

public class NewMessageLink
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "reply";

    [JsonPropertyName("mid")]
    public string? MessageId { get; set; }
}