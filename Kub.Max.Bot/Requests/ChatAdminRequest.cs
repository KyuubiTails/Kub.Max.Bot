using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на назначение администратора.

public class ChatAdminRequest
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}