using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на получение обновлений.

public class GetUpdatesRequest
{
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }

    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }
}