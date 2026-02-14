using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Базовая модель пользователя.

public class User
{
    [JsonPropertyName("user_id")]
    public long Id { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("last_activity_time")]
    public long? LastActivityTime { get; set; }

    [JsonPropertyName("name")]
    [Obsolete("Это поле устарело и будет удалено в будущих версиях.")]
    public string? Name { get; set; }
}