using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;

/// Информация о боте, возвращаемая методом GetMe.
public class BotInfo : UserWithPhoto
{
    [JsonPropertyName("commands")]
    public List<BotCommand>? Commands { get; set; }
}