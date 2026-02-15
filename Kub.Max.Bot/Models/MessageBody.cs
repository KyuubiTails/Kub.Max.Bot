using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;

/// <summary>
/// Тело сообщения.
/// </summary>
public class MessageBody
{
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }

    [JsonPropertyName("markup")]
    public List<MessageMarkup>? Markup { get; set; }
}

/// <summary>
/// Разметка текста.
/// </summary>
public class MessageMarkup
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }
}