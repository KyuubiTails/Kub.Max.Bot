using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;

/// <summary>
/// Универсальное вложение, используемое и для запросов, и для ответов
/// </summary>
public class Attachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    // Фабричные методы для создания разных типов вложений
    public static Attachment CreateFile(string token, string? url = null)
    {
        return new Attachment
        {
            Type = "file",
            Payload = new FilePayload { Token = token, Url = url }
        };
    }

    public static Attachment CreateImage(string token, string? url = null)
    {
        return new Attachment
        {
            Type = "image",
            Payload = new ImagePayload { Token = token, Url = url }
        };
    }

    public static Attachment CreateVideo(string token)
    {
        return new Attachment
        {
            Type = "video",
            Payload = new VideoPayload { Token = token }
        };
    }

    public static Attachment CreateAudio(string token)
    {
        return new Attachment
        {
            Type = "audio",
            Payload = new AudioPayload { Token = token }
        };
    }

    public static Attachment CreateKeyboard(List<List<InlineKeyboardButton>> buttons)
    {
        return new Attachment
        {
            Type = "inline_keyboard",
            Payload = new InlineKeyboardPayload { Buttons = buttons }
        };
    }

    // Методы для безопасного получения типизированного Payload
    public FilePayload? GetFilePayload() => Payload as FilePayload;
    public ImagePayload? GetImagePayload() => Payload as ImagePayload;
    public VideoPayload? GetVideoPayload() => Payload as VideoPayload;
    public AudioPayload? GetAudioPayload() => Payload as AudioPayload;
    public InlineKeyboardPayload? GetKeyboardPayload() => Payload as InlineKeyboardPayload;

    // Проверка типа
    public bool IsFile => Type == "file";
    public bool IsImage => Type == "image";
    public bool IsVideo => Type == "video";
    public bool IsAudio => Type == "audio";
    public bool IsKeyboard => Type == "inline_keyboard";
}

/// <summary>
/// Полезная нагрузка файла
/// </summary>
public class FilePayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("photos")]
    public List<string>? Photos { get; set; }
}

/// <summary>
/// Полезная нагрузка изображения
/// </summary>
public class ImagePayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Полезная нагрузка видео
/// </summary>
public class VideoPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Полезная нагрузка аудио
/// </summary>
public class AudioPayload
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Полезная нагрузка inline-клавиатуры
/// </summary>
public class InlineKeyboardPayload
{
    [JsonPropertyName("buttons")]
    public List<List<InlineKeyboardButton>> Buttons { get; set; } = new();
}

/// <summary>
/// Полезная нагрузка для загрузки изображения (для PatchChatRequest)
/// </summary>
public class PhotoAttachmentRequestPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}