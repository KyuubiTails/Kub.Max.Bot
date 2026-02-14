using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос вложения при отправке/редактировании сообщения.

public class AttachmentRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}


/// Запрос вложения файла.

public class FileAttachmentRequest : AttachmentRequest
{
    public FileAttachmentRequest()
    {
        Type = "file";
    }

    [JsonPropertyName("payload")]
    public new FileAttachmentPayload Payload { get; set; } = new();
}


/// Полезная нагрузка для вложения файла.

public class FileAttachmentPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}


/// Запрос вложения изображения.

public class ImageAttachmentRequest : AttachmentRequest
{
    public ImageAttachmentRequest()
    {
        Type = "image";
    }

    [JsonPropertyName("payload")]
    public new ImageAttachmentPayload Payload { get; set; } = new();
}


/// Полезная нагрузка для вложения изображения.

public class ImageAttachmentPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}


/// Запрос вложения видео.

public class VideoAttachmentRequest : AttachmentRequest
{
    public VideoAttachmentRequest()
    {
        Type = "video";
    }

    [JsonPropertyName("payload")]
    public new VideoAttachmentPayload Payload { get; set; } = new();
}


/// Полезная нагрузка для вложения видео.

public class VideoAttachmentPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}


/// Запрос вложения аудио.

public class AudioAttachmentRequest : AttachmentRequest
{
    public AudioAttachmentRequest()
    {
        Type = "audio";
    }

    [JsonPropertyName("payload")]
    public new AudioAttachmentPayload Payload { get; set; } = new();
}


/// Полезная нагрузка для вложения аудио.

public class AudioAttachmentPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}


/// Запрос вложения inline-клавиатуры.

public class InlineKeyboardAttachmentRequest : AttachmentRequest
{
    public InlineKeyboardAttachmentRequest()
    {
        Type = "inline_keyboard";
    }

    [JsonPropertyName("payload")]
    public new InlineKeyboardPayload Payload { get; set; } = new();
}