using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Models;


/// Кнопка inline-клавиатуры.

public class InlineKeyboardButton
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = InlineKeyboardButtonType.Callback;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; } = ButtonIntent.Default;

    [JsonPropertyName("quick")]
    public bool? Quick { get; set; }

    [JsonPropertyName("web_app")]
    public string? WebApp { get; set; }

    [JsonPropertyName("contact_id")]
    public long? ContactId { get; set; }
}


/// Типы кнопок inline-клавиатуры.

public static class InlineKeyboardButtonType
{
    public const string Callback = "callback";
    public const string Link = "link";
    public const string OpenApp = "open_app";
    public const string RequestGeoLocation = "request_geo_location";
    public const string RequestContact = "request_contact";
    public const string Message = "message";
}


/// Намерения кнопок (цвета).

public static class ButtonIntent
{
    public const string Positive = "positive";
    public const string Negative = "negative";
    public const string Default = "default";
}