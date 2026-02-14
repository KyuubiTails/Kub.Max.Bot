using Kub.Max.Bot.Requests;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на назначение администраторов чата.

public class AddChatAdminsRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("admins")]
    public List<ChatAdminRequest> Admins { get; set; } = new();
}