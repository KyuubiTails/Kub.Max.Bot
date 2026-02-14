using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Ответ на запрос получения участников чата.

public class GetChatMembersResponse : BaseResponse
{
    [JsonPropertyName("members")]
    public ChatMember[]? Members { get; set; }

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}