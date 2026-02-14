using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Результат загрузки файла.

public class FileUploadResult
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}