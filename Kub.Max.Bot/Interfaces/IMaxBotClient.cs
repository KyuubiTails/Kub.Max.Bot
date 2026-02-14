using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Kub.Max.Bot.Responses;

namespace Kub.Max.Bot.Interfaces;

/// Интерфейс клиента для работы с MAX Bot API.
public interface IMaxBotClient
{
    // ===== Bot =====
    /// Получает информацию о текущем боте.
    Task<BotInfo> GetMeAsync(CancellationToken cancellationToken = default);

    // ===== Users =====
    /// Получает информацию о пользователе.
    Task<UserWithPhoto> GetUserAsync(long userId, CancellationToken cancellationToken = default);

    // ===== Messages =====
    /// Отправляет сообщение.
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// Получает список сообщений.
    Task<GetMessagesResponse> GetMessagesAsync(GetMessagesRequest request, CancellationToken cancellationToken = default);

    /// Получает сообщение по идентификатору.
    Task<Message> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// Редактирует сообщение.
    Task<BaseResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default);

    /// Удаляет сообщение.
    Task<BaseResponse> DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

    // ===== Chats =====
    /// Получает список чатов.
    Task<GetChatsResponse> GetChatsAsync(GetChatsRequest request, CancellationToken cancellationToken = default);

    /// Получает информацию о чате.
    Task<Chat> GetChatInfoAsync(long chatId, CancellationToken cancellationToken = default);

    /// Изменяет информацию о чате.
    Task<Chat> PatchChatAsync(PatchChatRequest request, CancellationToken cancellationToken = default);

    /// Удаляет чат.
    Task<BaseResponse> DeleteChatAsync(long chatId, CancellationToken cancellationToken = default);

    /// Отправляет действие бота в чат.
    Task<BaseResponse> SendActionAsync(SendActionRequest request, CancellationToken cancellationToken = default);

    /// Получает закреплённое сообщение в чате.
    Task<Message?> GetPinnedMessageAsync(long chatId, CancellationToken cancellationToken = default);

    /// Закрепляет сообщение в чате.
    Task<BaseResponse> PinMessageAsync(long chatId, string messageId, bool? notify = null, CancellationToken cancellationToken = default);

    /// Открепляет сообщение в чате.
    Task<BaseResponse> UnpinMessageAsync(long chatId, CancellationToken cancellationToken = default);

    /// Получает информацию о членстве бота в чате.
    Task<ChatMember> GetMyChatMemberInfoAsync(long chatId, CancellationToken cancellationToken = default);

    /// Удаляет бота из чата.
    Task<BaseResponse> LeaveChatAsync(long chatId, CancellationToken cancellationToken = default);

    // ===== Chat Members =====
    /// Получает список администраторов чата.
    Task<GetChatMembersResponse> GetChatAdminsAsync(long chatId, CancellationToken cancellationToken = default);

    /// Назначает администраторов чата.
    Task<BaseResponse> AddChatAdminsAsync(AddChatAdminsRequest request, CancellationToken cancellationToken = default);

    /// Отменяет права администратора у пользователя.
    Task<BaseResponse> RemoveChatAdminAsync(long chatId, long userId, CancellationToken cancellationToken = default);

    /// Получает список участников чата.
    Task<GetChatMembersResponse> GetChatMembersAsync(GetChatMembersRequest request, CancellationToken cancellationToken = default);

    /// Добавляет участников в чат.
    Task<BaseResponse> AddChatMemberAsync(AddChatMemberRequest request, CancellationToken cancellationToken = default);

    /// Удаляет участника из чата.
    Task<BaseResponse> DeleteChatMemberAsync(DeleteChatMemberRequest request, CancellationToken cancellationToken = default);

    // ===== Callbacks =====
    /// Отвечает на callback от кнопки.
    Task<BaseResponse> AnswerCallbackAsync(string callbackId, AnswerCallbackRequest request, CancellationToken cancellationToken = default);

    // ===== Uploads =====
    /// Загружает файл на сервер.
    Task<FileUploadResult> UploadFileAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default);

    /// Загружает файл с использованием Multipart формы.
    Task<FileUploadResult> UploadFileMultipartAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default);

    // ===== Videos =====
    /// Получает информацию о видео.
    Task<VideoInfo> GetVideoInfoAsync(string videoToken, CancellationToken cancellationToken = default);

    // ===== Updates (Long Polling) =====
    /// Получает обновления через Long Polling.
    Task<GetUpdatesResponse> GetUpdatesAsync(GetUpdatesRequest request, CancellationToken cancellationToken = default);

    /// Запускает цикл Long Polling с обработчиком обновлений.
    Task RunPollingAsync(
        Func<Update, IMaxBotClient, Task> updateHandler,
        int? limit = 100,
        int? timeout = 30,
        int maxRetries = 5,
        CancellationToken cancellationToken = default);

    /// Останавливает Long Polling.
    void StopPolling();
}