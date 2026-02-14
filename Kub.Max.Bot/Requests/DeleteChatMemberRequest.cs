using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на удаление участника из чата.

public class DeleteChatMemberRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("block")]
    public bool? Block { get; set; }
}