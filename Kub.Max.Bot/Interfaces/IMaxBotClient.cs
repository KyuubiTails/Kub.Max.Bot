using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Kub.Max.Bot.Responses;

namespace Kub.Max.Bot.Interfaces;

/// <summary>
/// Интерфейс клиента для работы с MAX Bot API.
/// </summary>
public interface IMaxBotClient
{
    // ===== Bot =====
    /// <summary>
    /// Получает информацию о текущем боте.
    /// </summary>
    Task<BotInfo> GetMeAsync(CancellationToken cancellationToken = default);

    // ===== Users =====
    /// <summary>
    /// Получает информацию о пользователе.
    /// </summary>
    Task<UserWithPhoto> GetUserAsync(long userId, CancellationToken cancellationToken = default);

    // ===== Messages =====
    /// <summary>
    /// Отправляет сообщение.
    /// </summary>
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список сообщений.
    /// </summary>
    Task<GetMessagesResponse> GetMessagesAsync(GetMessagesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает сообщение по идентификатору.
    /// </summary>
    Task<Message> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Редактирует сообщение.
    /// </summary>
    Task<BaseResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сообщение.
    /// </summary>
    Task<BaseResponse> DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

    // ===== Chats =====
    /// <summary>
    /// Получает список чатов.
    /// </summary>
    Task<GetChatsResponse> GetChatsAsync(GetChatsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о чате.
    /// </summary>
    Task<Chat> GetChatInfoAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменяет информацию о чате.
    /// </summary>
    Task<Chat> PatchChatAsync(PatchChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет чат.
    /// </summary>
    Task<BaseResponse> DeleteChatAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправляет действие бота в чат.
    /// </summary>
    Task<BaseResponse> SendActionAsync(SendActionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает закреплённое сообщение в чате.
    /// </summary>
    Task<Message?> GetPinnedMessageAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Закрепляет сообщение в чате.
    /// </summary>
    Task<BaseResponse> PinMessageAsync(long chatId, string messageId, bool? notify = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Открепляет сообщение в чате.
    /// </summary>
    Task<BaseResponse> UnpinMessageAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о членстве бота в чате.
    /// </summary>
    Task<ChatMember> GetMyChatMemberInfoAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет бота из чата.
    /// </summary>
    Task<BaseResponse> LeaveChatAsync(long chatId, CancellationToken cancellationToken = default);

    // ===== Chat Members =====
    /// <summary>
    /// Получает список администраторов чата.
    /// </summary>
    Task<GetChatMembersResponse> GetChatAdminsAsync(long chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Назначает администраторов чата.
    /// </summary>
    Task<BaseResponse> AddChatAdminsAsync(AddChatAdminsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет права администратора у пользователя.
    /// </summary>
    Task<BaseResponse> RemoveChatAdminAsync(long chatId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список участников чата.
    /// </summary>
    Task<GetChatMembersResponse> GetChatMembersAsync(GetChatMembersRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет участников в чат.
    /// </summary>
    Task<BaseResponse> AddChatMemberAsync(AddChatMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет участника из чата.
    /// </summary>
    Task<BaseResponse> DeleteChatMemberAsync(DeleteChatMemberRequest request, CancellationToken cancellationToken = default);

    // ===== Callbacks =====
    /// <summary>
    /// Отвечает на callback от кнопки.
    /// </summary>
    Task<BaseResponse> AnswerCallbackAsync(string callbackId, AnswerCallbackRequest request, CancellationToken cancellationToken = default);

    // ===== Uploads =====
    /// <summary>
    /// Загружает файл на сервер.
    /// </summary>
    Task<FileUploadResult> UploadFileAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл с использованием Multipart формы.
    /// </summary>
    Task<FileUploadResult> UploadFileMultipartAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default);

    // ===== Videos =====
    /// <summary>
    /// Получает информацию о видео.
    /// </summary>
    Task<VideoInfo> GetVideoInfoAsync(string videoToken, CancellationToken cancellationToken = default);

    // ===== Webhooks =====
    /// <summary>
    /// Устанавливает вебхук для получения обновлений
    /// </summary>
    Task<BaseResponse> SetWebhookAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет вебхук
    /// </summary>
    Task<BaseResponse> DeleteWebhookAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает информацию о текущем вебхуке
    /// </summary>
    Task<WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default);

    // ===== Updates (Long Polling) =====
    /// <summary>
    /// Получает обновления через Long Polling.
    /// </summary>
    Task<GetUpdatesResponse> GetUpdatesAsync(GetUpdatesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Запускает цикл Long Polling с обработчиком обновлений.
    /// </summary>
    Task RunPollingAsync(
        Func<Update, IMaxBotClient, Task> updateHandler,
        Func<Exception, Update?, Task>? errorHandler = null,
        int? limit = 100,
        int? timeout = 30,
        int maxRetries = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Останавливает Long Polling.
    /// </summary>
    void StopPolling();
}