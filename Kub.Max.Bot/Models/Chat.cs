using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Модель чата.

public class Chat
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = ChatType.Chat;

    [JsonPropertyName("status")]
    public string Status { get; set; } = ChatStatus.Active;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("icon")]
    public Image? Icon { get; set; }

    [JsonPropertyName("last_event_time")]
    public long LastEventTime { get; set; }

    [JsonPropertyName("participants_count")]
    public int ParticipantsCount { get; set; }

    [JsonPropertyName("owner_id")]
    public long? OwnerId { get; set; }

    [JsonPropertyName("participants")]
    public object? Participants { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dialog_with_user")]
    public UserWithPhoto? DialogWithUser { get; set; }

    [JsonPropertyName("chat_message_id")]
    public string? ChatMessageId { get; set; }

    [JsonPropertyName("pinned_message")]
    public Message? PinnedMessage { get; set; }
}


/// Типы чатов.

public static class ChatType
{
    public const string Chat = "chat";
    public const string Dialog = "dialog";
    public const string Channel = "channel";
}


/// Статусы чатов.

public static class ChatStatus
{
    public const string Active = "active";
    public const string Removed = "removed";
    public const string Left = "left";
    public const string Closed = "closed";
}