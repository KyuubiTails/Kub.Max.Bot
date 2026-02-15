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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Kub.Max.Bot.Extensions;

namespace Kub.Max.Bot;

/// <summary>
/// Клиент для работы с MAX Bot API.
/// </summary>
public class MaxBotClient : IMaxBotClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<MaxBotClient> _logger;
    private CancellationTokenSource? _pollingCts;
    private bool _isPolling;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Создаёт новый экземпляр клиента с указанным токеном.
    /// </summary>
    public MaxBotClient(string token, string? baseUrl = null, HttpClient? httpClient = null, ILogger<MaxBotClient>? logger = null)
    {
        _baseUrl = baseUrl ?? "https://platform-api.max.ru";
        _logger = logger ?? NullLogger<MaxBotClient>.Instance;
        _ownsHttpClient = httpClient == null;

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", token);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Создаёт новый экземпляр клиента с указанным токеном и таймаутом.
    /// </summary>
    public MaxBotClient(string token, TimeSpan timeout, string? baseUrl = null, ILogger<MaxBotClient>? logger = null)
        : this(token, baseUrl, null, logger)
    {
        _httpClient.Timeout = timeout;
    }

    /// <summary>
    /// Отправляет HTTP-запрос и возвращает десериализованный ответ.
    /// </summary>
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

            _logger.LogDebug("Request to {Method} {Endpoint}: {Json}", method, endpoint, json);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Response from {Method} {Endpoint}: Status {StatusCode}, Body: {Body}",
                method, endpoint, response.StatusCode, responseContent);

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

            var result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            if (result == null)
            {
                throw new InvalidOperationException("Не удалось десериализовать ответ");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed to {Method} {Endpoint}", method, endpoint);
            throw new MaxBotClientException($"HTTP request failed: {ex.Message}", HttpStatusCode.ServiceUnavailable, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request timeout to {Method} {Endpoint}", method, endpoint);
            throw new MaxBotClientException("Request timeout", HttpStatusCode.RequestTimeout, ex);
        }
    }

    /// <summary>
    /// Формирует строку запроса из словаря параметров.
    /// </summary>
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
            Attachments = request.Attachments, // Теперь напрямую используем Attachment
            Link = request.Link,
            Notify = request.Notify,
            Format = request.Format?.ToString().ToLowerInvariant()
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

    // ===== Webhooks =====

    public async Task<BaseResponse> SetWebhookAsync(string url, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Post,
            "/webhook",
            new { url },
            cancellationToken);
    }

    public async Task<BaseResponse> DeleteWebhookAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<BaseResponse>(
            HttpMethod.Delete,
            "/webhook",
            null,
            cancellationToken);
    }

    public async Task<WebhookInfo> GetWebhookInfoAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<WebhookInfo>(
            HttpMethod.Get,
            "/webhook",
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

    /// <summary>
    /// Запускает цикл Long Polling с обработчиком обновлений.
    /// </summary>
    public async Task RunPollingAsync(
        Func<Update, IMaxBotClient, Task> updateHandler,
        Func<Exception, Update?, Task>? errorHandler = null,
        int? limit = 100,
        int? timeout = 30,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
    {
        if (_isPolling)
        {
            throw new InvalidOperationException("Polling is already running");
        }

        _isPolling = true;
        _pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        int retryCount = 0;
        long? currentMarker = null;

        _logger.LogInformation("Starting Long Polling with limit={Limit}, timeout={Timeout}", limit, timeout);

        try
        {
            while (!_pollingCts.Token.IsCancellationRequested && retryCount < maxRetries)
            {
                try
                {
                    var updatesRequest = new GetUpdatesRequest
                    {
                        Limit = limit,
                        Timeout = timeout,
                        Marker = currentMarker
                    };

                    var response = await GetUpdatesAsync(updatesRequest, _pollingCts.Token);

                    if (response.Updates?.Any() == true)
                    {
                        _logger.LogDebug("Received {Count} updates", response.Updates.Count);

                        foreach (var update in response.Updates)
                        {
                            try
                            {
                                await updateHandler(update, this);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error in update handler for update type {UpdateType}", update.UpdateType);

                                if (errorHandler != null)
                                {
                                    await errorHandler(ex, update);
                                }
                            }
                        }
                    }

                    currentMarker = response.Marker;
                    retryCount = 0;
                }
                catch (OperationCanceledException) when (_pollingCts.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Long Polling cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, "Error in Long Polling ({RetryCount}/{MaxRetries})", retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError("Max retries reached, stopping Long Polling");
                        throw;
                    }

                    if (errorHandler != null)
                    {
                        await errorHandler(ex, null);
                    }

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    _logger.LogInformation("Waiting {Delay} before retry", delay);

                    try
                    {
                        await Task.Delay(delay, _pollingCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            _isPolling = false;
        }
    }

    /// <summary>
    /// Останавливает Long Polling.
    /// </summary>
    public void StopPolling()
    {
        if (_isPolling)
        {
            _logger.LogInformation("Stopping Long Polling");
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
            _isPolling = false;
        }
    }

    // ===== IDisposable =====

    public void Dispose()
    {
        StopPolling();

        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }

        _pollingCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Информация о вебхуке
/// </summary>
public class WebhookInfo
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("has_custom_certificate")]
    public bool HasCustomCertificate { get; set; }

    [JsonPropertyName("pending_update_count")]
    public int PendingUpdateCount { get; set; }

    [JsonPropertyName("last_error_date")]
    public long? LastErrorDate { get; set; }

    [JsonPropertyName("last_error_message")]
    public string? LastErrorMessage { get; set; }
}