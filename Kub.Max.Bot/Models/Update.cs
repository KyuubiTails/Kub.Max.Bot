using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Обновление (событие).

public class Update
{
    [JsonPropertyName("update_type")]
    public string UpdateType { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    [JsonPropertyName("callback")]
    public Callback? Callback { get; set; }

    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("chat_id")]
    public long? ChatId { get; set; }

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}


/// Типы обновлений.

public static class UpdateTypes
{
    public const string MessageCreated = "message_created";
    public const string MessageCallback = "message_callback";
    public const string MessageEdited = "message_edited";
    public const string MessageDeleted = "message_deleted";
    public const string ChatMemberJoined = "chat_member_joined";
    public const string ChatMemberLeft = "chat_member_left";
    public const string ChatTitleChanged = "chat_title_changed";
    public const string BotStarted = "bot_started";
    public const string BotStopped = "bot_stopped";
}