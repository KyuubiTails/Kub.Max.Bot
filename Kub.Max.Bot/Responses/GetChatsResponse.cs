using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Ответ на запрос получения списка чатов.

public class GetChatsResponse : BaseResponse
{
    [JsonPropertyName("chats")]
    public Chat[]? Chats { get; set; }

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}