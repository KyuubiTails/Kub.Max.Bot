using Kub.Max.Bot.Exceptions;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Kub.Max.Bot.Responses;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Kub.Max.Bot;


/// Клиент для работы с MAX Bot API.

public class MaxBotClient : IMaxBotClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://platform-api.max.ru";
    private readonly JsonSerializerOptions _jsonOptions;
    private CancellationTokenSource? _pollingCts;
    private bool _isPolling;

    
    /// Создаёт новый экземпляр клиента с указанным токеном.
    
    public MaxBotClient(string token, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", token);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    
    /// Создаёт новый экземпляр клиента с указанным токеном и таймаутом.
    
    public MaxBotClient(string token, TimeSpan timeout)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = timeout
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", token);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    
    /// Отправляет HTTP-запрос и возвращает десериализованный ответ.
    
    private async Task<T> SendRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = endpoint.StartsWith("http")
            ? new Uri(endpoint)
            : new Uri(_httpClient.BaseAddress!, endpoint);

        using var request = new HttpRequestMessage(method, requestUri);

        if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MaxBotClientException(
                $"HTTP {response.StatusCode}: {responseContent}",
                response.StatusCode);
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)responseContent;
        }

        return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions)
            ?? throw new InvalidOperationException("Не удалось десериализовать ответ");
    }

    
    /// Формирует строку запроса из словаря параметров.
    
    private string BuildQueryString(Dictionary<string, object?> parameters)
    {
        var queryParams = parameters
            .Where(p => p.Value != null)
            .Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value!.ToString())}");

        return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
    }

    // ===== Bot =====

    public async Task<BotInfo> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BotInfo>(HttpMethod.Get, "/me", null, cancellationToken);
    }

    // ===== Users =====

    public async Task<UserWithPhoto> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<UserWithPhoto>(HttpMethod.Get, $"/users/{userId}", null, cancellationToken);
    }

    // ===== Messages =====

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, object?>();

        if (request.UserId.HasValue)
            queryParams["user_id"] = request.UserId.Value;

        if (request.ChatId.HasValue)
            queryParams["chat_id"] = request.ChatId.Value;

        if (request.DisableLinkPreview.HasValue)
            queryParams["disable_link_preview"] = request.DisableLinkPreview.Value.ToString().ToLower();

        var queryString = BuildQueryString(queryParams);

        var messageBody = new NewMessageBody
        {
            Text = request.Text,
            Attachments = request.Attachments,
            Link = request.Link,
            Notify = request.Notify,
            Format = request.Format?.ToString().ToLower()
        };

        return await SendRequestAsync<SendMessageResponse>(
            HttpMethod.Post,
            $"/messages{queryString}",
            messageBody,
            cancellationToken);
    }

    public async Task<GetMessagesResponse> GetMessagesAsync(GetMessagesRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, object?>();

        if (request.ChatId.HasValue)
            queryParams["chat_id"] = request.ChatId.Value;

        if (request.MessageIds?.Any() == true)
            queryParams["message_ids"] = string.Join(",", request.MessageIds);

        if (request.From.HasValue)
            queryParams["from"] = request.From.Value;

        if (request.To.HasValue)
            queryParams["to"] = request.To.Value;

        if (request.Count.HasValue)
            queryParams["count"] = request.Count.Value;

        var queryString = BuildQueryString(queryParams);

        return await SendRequestAsync<GetMessagesResponse>(
            HttpMethod.Get,
            $"/messages{queryString}",
            null,
            cancellationToken);
    }

    public async Task<Message> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Message>(
            HttpMethod.Get,
            $"/messages/{messageId}",
            null,
            cancellationToken);
    }

    public async Task<BaseResponse> EditMessageAsync(EditMessageRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = BuildQueryString(new Dictionary<string, object?>
        {
            ["message_id"] = request.MessageId
        });

        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Put,
            $"/messages{queryParams}",
            request,
            cancellationToken);
    }

    public async Task<BaseResponse> DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var queryParams = BuildQueryString(new Dictionary<string, object?>
        {
            ["message_id"] = messageId
        });

        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/messages{queryParams}",
            null,
            cancellationToken);
    }

    // ===== Chats =====

    public async Task<GetChatsResponse> GetChatsAsync(GetChatsRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, object?>();

        if (request.Count.HasValue)
            queryParams["count"] = request.Count.Value;

        if (request.Marker.HasValue)
            queryParams["marker"] = request.Marker.Value;

        var queryString = BuildQueryString(queryParams);

        return await SendRequestAsync<GetChatsResponse>(
            HttpMethod.Get,
            $"/chats{queryString}",
            null,
            cancellationToken);
    }

    public async Task<Chat> GetChatInfoAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Chat>(
            HttpMethod.Get,
            $"/chats/{chatId}",
            null,
            cancellationToken);
    }

    public async Task<Chat> PatchChatAsync(PatchChatRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Chat>(
            HttpMethod.Patch,
            $"/chats/{request.ChatId}",
            request,
            cancellationToken);
    }

    public async Task<BaseResponse> DeleteChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/chats/{chatId}",
            null,
            cancellationToken);
    }

    public async Task<BaseResponse> SendActionAsync(SendActionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Post,
            $"/chats/{request.ChatId}/actions",
            request,
            cancellationToken);
    }

    public async Task<Message?> GetPinnedMessageAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync<Dictionary<string, Message?>>(
            HttpMethod.Get,
            $"/chats/{chatId}/pin",
            null,
            cancellationToken);

        return response.GetValueOrDefault("message");
    }

    public async Task<BaseResponse> PinMessageAsync(long chatId, string messageId, bool? notify = null, CancellationToken cancellationToken = default)
    {
        var request = new { message_id = messageId, notify };
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Put,
            $"/chats/{chatId}/pin",
            request,
            cancellationToken);
    }

    public async Task<BaseResponse> UnpinMessageAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/chats/{chatId}/pin",
            null,
            cancellationToken);
    }

    public async Task<ChatMember> GetMyChatMemberInfoAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ChatMember>(
            HttpMethod.Get,
            $"/chats/{chatId}/members/me",
            null,
            cancellationToken);
    }

    public async Task<BaseResponse> LeaveChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/chats/{chatId}/members/me",
            null,
            cancellationToken);
    }

    // ===== Chat Members =====

    public async Task<GetChatMembersResponse> GetChatAdminsAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<GetChatMembersResponse>(
            HttpMethod.Get,
            $"/chats/{chatId}/members/admins",
            null,
            cancellationToken);
    }

    public async Task<BaseResponse> AddChatAdminsAsync(AddChatAdminsRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Post,
            $"/chats/{request.ChatId}/members/admins",
            request,
            cancellationToken);
    }

    public async Task<BaseResponse> RemoveChatAdminAsync(long chatId, long userId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/chats/{chatId}/members/admins/{userId}",
            null,
            cancellationToken);
    }

    public async Task<GetChatMembersResponse> GetChatMembersAsync(GetChatMembersRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, object?>();

        if (request.UserIds?.Any() == true)
            queryParams["user_ids"] = string.Join(",", request.UserIds);

        if (request.Marker.HasValue)
            queryParams["marker"] = request.Marker.Value;

        if (request.Count.HasValue)
            queryParams["count"] = request.Count.Value;

        var queryString = BuildQueryString(queryParams);

        return await SendRequestAsync<GetChatMembersResponse>(
            HttpMethod.Get,
            $"/chats/{request.ChatId}/members{queryString}",
            null,
            cancellationToken);
    }

    public async Task<BaseResponse> AddChatMemberAsync(AddChatMemberRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Post,
            $"/chats/{request.ChatId}/members",
            request,
            cancellationToken);
    }

    public async Task<BaseResponse> DeleteChatMemberAsync(DeleteChatMemberRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = BuildQueryString(new Dictionary<string, object?>
        {
            ["user_id"] = request.UserId,
            ["block"] = request.Block
        });

        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            $"/chats/{request.ChatId}/members{queryParams}",
            null,
            cancellationToken);
    }

    // ===== Callbacks =====

    public async Task<BaseResponse> AnswerCallbackAsync(string callbackId, AnswerCallbackRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = BuildQueryString(new Dictionary<string, object?>
        {
            ["callback_id"] = callbackId
        });

        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Post,
            $"/answers{queryParams}",
            request,
            cancellationToken);
    }

    // ===== Uploads =====

    public async Task<FileUploadResult> UploadFileAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default)
    {
        var uploadUrlResponse = await SendRequestAsync<UploadUrlResponse>(
            HttpMethod.Post,
            $"/uploads?type={type}",
            null,
            cancellationToken);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileData), "data", fileName);

        var response = await _httpClient.PostAsync(uploadUrlResponse.Url, content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MaxBotClientException(
                $"HTTP {response.StatusCode}: {responseContent}",
                response.StatusCode);
        }

        return JsonSerializer.Deserialize<FileUploadResult>(responseContent, _jsonOptions)
            ?? throw new InvalidOperationException("Не удалось десериализовать результат загрузки");
    }

    public async Task<FileUploadResult> UploadFileMultipartAsync(byte[] fileData, string fileName, string type, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileData), "data", fileName);

        var response = await _httpClient.PostAsync($"/uploads?type={type}", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MaxBotClientException(
                $"HTTP {response.StatusCode}: {responseContent}",
                response.StatusCode);
        }

        return JsonSerializer.Deserialize<FileUploadResult>(responseContent, _jsonOptions)
            ?? throw new InvalidOperationException("Не удалось десериализовать результат загрузки");
    }

    private class UploadUrlResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    // ===== Videos =====

    public async Task<VideoInfo> GetVideoInfoAsync(string videoToken, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<VideoInfo>(
            HttpMethod.Get,
            $"/videos/{videoToken}",
            null,
            cancellationToken);
    }

    // ===== Updates (Long Polling) =====

    public async Task<GetUpdatesResponse> GetUpdatesAsync(GetUpdatesRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, object?>();

        if (request.Limit.HasValue)
            queryParams["limit"] = request.Limit.Value;

        if (request.Timeout.HasValue)
            queryParams["timeout"] = request.Timeout.Value;

        if (request.Marker.HasValue)
            queryParams["marker"] = request.Marker.Value;

        if (request.Types?.Any() == true)
            queryParams["types"] = string.Join(",", request.Types);

        var queryString = BuildQueryString(queryParams);

        return await SendRequestAsync<GetUpdatesResponse>(
            HttpMethod.Get,
            $"/updates{queryString}",
            null,
            cancellationToken);
    }

    
    /// Запускает цикл Long Polling с обработчиком обновлений.
    
    public async Task RunPollingAsync(
        Func<Update, IMaxBotClient, Task> updateHandler,
        int? limit = 100,
        int? timeout = 30,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        long? currentMarker = null;

        while (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                var updatesRequest = new GetUpdatesRequest
                {
                    Limit = limit,
                    Timeout = timeout,
                    Marker = currentMarker
                };

                var response = await GetUpdatesAsync(updatesRequest, cancellationToken);

                foreach (var update in response.Updates)
                {
                    try
                    {
                        await updateHandler(update, this);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка в обработчике обновления: {ex.Message}");
                    }
                }

                currentMarker = response.Marker;
                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"Ошибка в Long Polling ({retryCount}/{maxRetries}): {ex.Message}");

                if (retryCount >= maxRetries)
                    throw;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    
    /// Останавливает Long Polling.
    
    public void StopPolling()
    {
        _isPolling = false;
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    // ===== IDisposable =====

    public void Dispose()
    {
        StopPolling();
        _httpClient?.Dispose();
        _pollingCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}