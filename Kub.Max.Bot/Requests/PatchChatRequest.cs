using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на изменение информации о чате.

public class PatchChatRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("icon")]
    public PhotoAttachmentRequestPayload? Icon { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("pin")]
    public string? PinMessageId { get; set; }

    [JsonPropertyName("notify")]
    public bool? Notify { get; set; } = true;
}