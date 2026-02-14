using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на загрузку файла.

public class UploadFileRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "file";
}


/// Типы загружаемых файлов.

public static class UploadType
{
    public const string Image = "image";
    public const string Video = "video";
    public const string Audio = "audio";
    public const string File = "file";
}