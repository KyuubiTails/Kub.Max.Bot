using Kub.Max.Bot.Models;
using System.Text.Json.Serialization;

namespace Kub.Max.Bot.Requests;


/// Запрос на отправку сообщения.

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
    public List<AttachmentRequest>? Attachments { get; set; }

    [JsonPropertyName("link")]
    public NewMessageLink? Link { get; set; }

    [JsonPropertyName("notify")]
    public bool? Notify { get; set; } = true;

    [JsonPropertyName("format")]
    public MessageFormat? Format { get; set; }

    
    /// Создаёт простое текстовое сообщение.
    
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

    
    /// Создаёт сообщение с inline-клавиатурой.
    
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
            Attachments = new List<AttachmentRequest>
            {
                new InlineKeyboardAttachmentRequest
                {
                    Payload = new InlineKeyboardPayload
                    {
                        Buttons = keyboard
                    }
                }
            }
        };
    }
}