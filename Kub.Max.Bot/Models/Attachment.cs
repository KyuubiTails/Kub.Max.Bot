using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Базовое вложение сообщения.

public class Attachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}


/// Вложение файла.

public class FileAttachment : Attachment
{
    public FileAttachment()
    {
        Type = "file";
    }

    [JsonPropertyName("payload")]
    public new FilePayload Payload { get; set; } = new();
}


/// Полезная нагрузка файла.

public class FilePayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("photos")]
    public List<string>? Photos { get; set; }
}


/// Вложение изображения.

public class ImageAttachment : Attachment
{
    public ImageAttachment()
    {
        Type = "image";
    }

    [JsonPropertyName("payload")]
    public new ImagePayload Payload { get; set; } = new();
}


/// Полезная нагрузка изображения.

public class ImagePayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}


/// Вложение видео.

public class VideoAttachment : Attachment
{
    public VideoAttachment()
    {
        Type = "video";
    }

    [JsonPropertyName("payload")]
    public new VideoPayload Payload { get; set; } = new();
}


/// Полезная нагрузка видео.

public class VideoPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}


/// Вложение аудио.

public class AudioAttachment : Attachment
{
    public AudioAttachment()
    {
        Type = "audio";
    }

    [JsonPropertyName("payload")]
    public new AudioPayload Payload { get; set; } = new();
}


/// Полезная нагрузка аудио.

public class AudioPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}


/// Вложение inline-клавиатуры.

public class InlineKeyboardAttachment : Attachment
{
    public InlineKeyboardAttachment()
    {
        Type = "inline_keyboard";
    }

    [JsonPropertyName("payload")]
    public new InlineKeyboardPayload Payload { get; set; } = new();
}


/// Полезная нагрузка inline-клавиатуры.

public class InlineKeyboardPayload
{
    [JsonPropertyName("buttons")]
    public List<List<InlineKeyboardButton>> Buttons { get; set; } = new();
}


/// Полезная нагрузка для загрузки изображения.

public class PhotoAttachmentRequestPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}