using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;

/// <summary>
/// Запрос на ответ на callback.
/// </summary>
public class AnswerCallbackRequest
{
    [JsonPropertyName("message")]
    public NewMessageBody? Message { get; set; }

    [JsonPropertyName("notification")]
    public string? Notification { get; set; }
}