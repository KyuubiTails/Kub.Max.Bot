using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на получение сообщений.

public class GetMessagesRequest
{
    [JsonPropertyName("chat_id")]
    public long? ChatId { get; set; }

    [JsonPropertyName("message_ids")]
    public List<string>? MessageIds { get; set; }

    [JsonPropertyName("from")]
    public long? From { get; set; }

    [JsonPropertyName("to")]
    public long? To { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}