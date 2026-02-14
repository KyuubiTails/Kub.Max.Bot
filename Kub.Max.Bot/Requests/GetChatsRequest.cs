using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на получение списка чатов.

public class GetChatsRequest
{
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}