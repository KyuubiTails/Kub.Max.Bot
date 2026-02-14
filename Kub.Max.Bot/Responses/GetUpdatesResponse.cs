using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Responses;


/// Ответ на запрос получения обновлений.

public class GetUpdatesResponse : BaseResponse
{
    [JsonPropertyName("updates")]
    public List<Update> Updates { get; set; } = new();

    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}