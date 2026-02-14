using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Ответ на запрос получения сообщений.

public class GetMessagesResponse : BaseResponse
{
    [JsonPropertyName("messages")]
    public Message[]? Messages { get; set; }
}