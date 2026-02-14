using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на получение участников чата.

public class GetChatMembersRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("user_ids")]
    public long[]? UserIds { get; set; }

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}