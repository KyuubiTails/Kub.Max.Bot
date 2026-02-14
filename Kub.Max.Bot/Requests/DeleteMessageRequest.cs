using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на удаление сообщения.

public class DeleteMessageRequest
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;
}