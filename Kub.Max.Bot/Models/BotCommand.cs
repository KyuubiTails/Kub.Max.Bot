using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;

/// Команда бота.
public class BotCommand
{
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}