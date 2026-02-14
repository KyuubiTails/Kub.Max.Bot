using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Базовый ответ от API.

public class BaseResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}