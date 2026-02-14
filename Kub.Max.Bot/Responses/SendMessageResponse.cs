using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Ответ на отправку сообщения.

public class SendMessageResponse : BaseResponse
{
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}