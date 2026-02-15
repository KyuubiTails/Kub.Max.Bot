using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;

/// <summary>
/// Запрос на отправку сообщения.
/// </summary>
public class SendMessageRequest
{
    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("chat_id")]
    public long? ChatId { get; set; }

    [JsonPropertyName("disable_link_preview")]
    public bool? DisableLinkPreview { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }

    [JsonPropertyName("link")]
    public NewMessageLink? Link { get; set; }

    [JsonPropertyName("notify")]
    public bool? Notify { get; set; } = true;

    [JsonPropertyName("format")]
    public MessageFormat? Format { get; set; }

    /// <summary>
    /// Создаёт простое текстовое сообщение.
    /// </summary>
    public static SendMessageRequest CreateText(
        long chatId,
        string text,
        MessageFormat format = MessageFormat.Markdown,
        bool disableLinkPreview = false)
    {
        return new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            Format = format,
            DisableLinkPreview = disableLinkPreview
        };
    }

    /// <summary>
    /// Создаёт сообщение с inline-клавиатурой.
    /// </summary>
    public static SendMessageRequest CreateWithKeyboard(
        long chatId,
        string text,
        List<List<InlineKeyboardButton>> keyboard,
        MessageFormat format = MessageFormat.Markdown,
        bool disableLinkPreview = false)
    {
        return new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            Format = format,
            DisableLinkPreview = disableLinkPreview,
            Attachments = new List<Attachment>
            {
                Attachment.CreateKeyboard(keyboard)
            }
        };
    }

    /// <summary>
    /// Создаёт сообщение с изображением.
    /// </summary>
    public static SendMessageRequest CreateWithImage(
        long chatId,
        string text,
        string imageToken,
        MessageFormat format = MessageFormat.Markdown)
    {
        return new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            Format = format,
            Attachments = new List<Attachment>
            {
                Attachment.CreateImage(imageToken)
            }
        };
    }

    /// <summary>
    /// Создаёт сообщение с файлом.
    /// </summary>
    public static SendMessageRequest CreateWithFile(
        long chatId,
        string text,
        string fileToken,
        MessageFormat format = MessageFormat.Markdown)
    {
        return new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            Format = format,
            Attachments = new List<Attachment>
            {
                Attachment.CreateFile(fileToken)
            }
        };
    }

    /// <summary>
    /// Создаёт сообщение с видео.
    /// </summary>
    public static SendMessageRequest CreateWithVideo(
        long chatId,
        string text,
        string videoToken,
        MessageFormat format = MessageFormat.Markdown)
    {
        return new SendMessageRequest
        {
            ChatId = chatId,
            Text = text,
            Format = format,
            Attachments = new List<Attachment>
            {
                Attachment.CreateVideo(videoToken)
            }
        };
    }
}