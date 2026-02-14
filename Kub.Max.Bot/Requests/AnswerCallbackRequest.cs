using Kub.Max.Bot.Requests;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на ответ на callback.

public class AnswerCallbackRequest
{
    [JsonPropertyName("message")]
    public NewMessageBody? Message { get; set; }

    [JsonPropertyName("notification")]
    public string? Notification { get; set; }
}