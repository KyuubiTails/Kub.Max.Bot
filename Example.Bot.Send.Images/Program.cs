using Kub.Max.Bot;
using Kub.Max.Bot.Extensions;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MediaBot;

class MediaBot
{
    private readonly IMaxBotClient _botClient;
    private readonly ILogger<MediaBot> _logger;
    private readonly Dictionary<long, string> _userStates = new();

    public MediaBot(IMaxBotClient botClient, ILogger<MediaBot> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        var botInfo = await _botClient.GetMeAsync();
        _logger.LogInformation("Медиа-бот {BotName} запущен", botInfo.FirstName);

        await _botClient.RunPollingAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync
        );
    }

    private async Task HandleUpdateAsync(Update update, IMaxBotClient botClient)
    {
        try
        {
            if (update.UpdateType == UpdateTypes.MessageCreated && update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
            else if (update.UpdateType == UpdateTypes.MessageCallback && update.Callback != null)
            {
                await HandleCallbackAsync(update.Callback);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления");
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Recipient?.ChatId ?? 0;
        var userId = message.Sender?.Id ?? 0;
        var text = message.Body?.Text?.Trim() ?? "";

        if (chatId == 0)
        {
            _logger.LogWarning("Получено сообщение с chatId = 0");
            return;
        }

        // Проверяем, есть ли вложения в полученном сообщении
        if (message.Body?.Attachments?.Any() == true)
        {
            await HandleAttachmentsAsync(chatId, message.Body.Attachments);
            return;
        }

        switch (text.ToLower())
        {
            case "/start":
                await SendWelcomeMessage(chatId);
                break;

            case "/help":
                await SendHelpMessage(chatId);
                break;

            default:
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    chatId,
                    "❓ Отправьте мне любой файл, и я покажу его параметры!"
                ));
                break;
        }
    }

    private async Task HandleCallbackAsync(Callback callback)
    {
        var chatId = callback.ChatId ?? 0;
        var payload = callback.Payload;

        if (chatId == 0) return;

        await _botClient.AnswerCallbackAsync(callback.CallbackId, new AnswerCallbackRequest
        {
            Notification = "✅ Обрабатываю..."
        });

        if (payload == "help")
        {
            await SendHelpMessage(chatId);
        }
    }

    private async Task SendWelcomeMessage(long chatId)
    {
        var welcomeMessage =
            "📁 *Медиа-бот*\n\n" +
            "Я могу показать информацию о любом файле, который вы отправите!\n\n" +
            "📌 *Команды:*\n" +
            "• `/help` - показать справку\n\n" +
            "Просто отправь мне любой файл или изображение!";

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "❓ Помощь",
                    Payload = "help",
                    Intent = ButtonIntent.Default
                }
            }
        };

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(chatId, welcomeMessage, keyboard)
        );
    }

    private async Task SendHelpMessage(long chatId)
    {
        var helpText =
            "📚 *Справка*\n\n" +
            "**Как работать с ботом:**\n" +
            "1. Отправьте мне любой файл (изображение, документ, видео)\n" +
            "2. Я покажу всю информацию о файле\n" +
            "3. Вы увидите токен, URL и другие параметры\n\n" +
            "**Поддерживаемые типы файлов:**\n" +
            "• Изображения (JPEG, PNG, GIF)\n" +
            "• Документы (PDF, DOC, TXT)\n" +
            "• Видео и аудио\n" +
            "• И другие";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, helpText));
    }

    private async Task HandleAttachmentsAsync(long chatId, List<Attachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            // Преобразуем Payload в JsonElement для детального разбора
            var payloadJson = JsonSerializer.Serialize(attachment.Payload);
            var payloadElement = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            var responseText = "📦 *Получено вложение*\n\n";
            responseText += $"**Тип:** {attachment.Type}\n\n";

            // Добавляем все поля из payload
            if (payloadElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in payloadElement.EnumerateObject())
                {
                    string value = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? ""
                        : property.Value.ToString();

                    responseText += $"**{property.Name}:** `{value}`\n";
                }
            }

            // Добавляем сырой JSON для отладки
            responseText += $"\n**Сырой JSON:**\n```json\n{payloadJson}\n```";

            await _botClient.SendMessageAsync(
                SendMessageRequest.CreateText(chatId, responseText)
            );

            // Пытаемся найти токен или URL в payload
            string? token = null;
            string? url = null;

            if (payloadElement.ValueKind == JsonValueKind.Object)
            {
                if (payloadElement.TryGetProperty("token", out var tokenProp))
                    token = tokenProp.GetString();

                if (payloadElement.TryGetProperty("url", out var urlProp))
                    url = urlProp.GetString();

                if (payloadElement.TryGetProperty("file_id", out var fileIdProp))
                    token = fileIdProp.GetString();
            }

            // Если нашли токен, показываем пример использования
            if (!string.IsNullOrEmpty(token))
            {
                await ShowTokenExample(chatId, token, attachment.Type);
            }
            else if (!string.IsNullOrEmpty(url))
            {
                await ShowUrlExample(chatId, url, attachment.Type);
            }
        }
    }

    private async Task ShowTokenExample(long chatId, string token, string fileType)
    {
        var exampleMessage =
            "📋 *Найден токен!*\n\n" +
            "Вы можете использовать его для отправки этого файла:\n\n";

        if (fileType == "image" || fileType == "photo" || fileType.Contains("image"))
        {
            exampleMessage +=
                "```csharp\n" +
                "await botClient.SendMessageAsync(\n" +
                "    SendMessageRequest.CreateWithImage(\n" +
                $"        chatId: 123456789,\n" +
                "        text: \"Вот изображение\",\n" +
                $"        imageToken: \"{token}\"\n" +
                "    )\n" +
                ");\n```";
        }
        else
        {
            exampleMessage +=
                "```csharp\n" +
                "await botClient.SendMessageAsync(\n" +
                "    SendMessageRequest.CreateWithFile(\n" +
                $"        chatId: 123456789,\n" +
                "        text: \"Вот файл\",\n" +
                $"        fileToken: \"{token}\"\n" +
                "    )\n" +
                ");\n```";
        }

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, exampleMessage));
    }

    private async Task ShowUrlExample(long chatId, string url, string fileType)
    {
        var exampleMessage =
            "🔗 *Найден URL!*\n\n" +
            $"**Ссылка:** {url}\n\n" +
            "Вы можете открыть её в браузере или использовать в коде:\n\n" +
            "```csharp\n" +
            $"var fileBytes = await httpClient.GetByteArrayAsync(\"{url}\");\n" +
            "```";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, exampleMessage));
    }

    private Task HandleErrorAsync(Exception exception, Update? update)
    {
        _logger.LogError(exception, "Ошибка в медиа-боте");
        return Task.CompletedTask;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            var bot = host.Services.GetRequiredService<MediaBot>();

            Console.WriteLine("🚀 Запуск медиа-бота...");
            await bot.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddMaxBotClient(options =>
                {
                    options.Token = "YOUR_BOT_TOKEN"; // Замените на свой токен
                    options.Timeout = TimeSpan.FromSeconds(60);
                });

                services.AddSingleton<MediaBot>();
                services.AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(LogLevel.Information);
                });
            })
            .UseConsoleLifetime();
}