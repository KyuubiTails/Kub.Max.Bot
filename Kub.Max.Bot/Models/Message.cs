using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Сообщение.

public class Message
{
    [JsonPropertyName("sender")]
    public User? Sender { get; set; }

    [JsonPropertyName("recipient")]
    public Recipient? Recipient { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("link")]
    public LinkedMessage? Link { get; set; }

    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }

    [JsonPropertyName("stat")]
    public MessageStat? Stat { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}


/// Получатель сообщения.

public class Recipient
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }
}


/// Связанное сообщение (пересланное или ответное).

public class LinkedMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mid")]
    public string? MessageId { get; set; }

    [JsonPropertyName("sender")]
    public User? Sender { get; set; }

    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }
}


/// Статистика сообщения.

public class MessageStat
{
    [JsonPropertyName("views")]
    public int? Views { get; set; }
}