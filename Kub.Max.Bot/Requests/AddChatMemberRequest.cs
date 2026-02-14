using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на добавление участников в чат.

public class AddChatMemberRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("user_ids")]
    public long[] UserIds { get; set; } = Array.Empty<long>();
}