using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на отправку действия бота.

public class SendActionRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
}


/// Действия бота.

public static class SenderAction
{
    public const string TypingOn = "typing_on";
    public const string SendingPhoto = "sending_photo";
    public const string SendingVideo = "sending_video";
    public const string SendingAudio = "sending_audio";
    public const string SendingFile = "sending_file";
    public const string MarkSeen = "mark_seen";
}