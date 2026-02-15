using Kub.Max.Bot;
using Kub.Max.Bot.Extensions;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommandBot;

class CommandBot
{
    private readonly IMaxBotClient _botClient;
    private readonly ILogger<CommandBot> _logger;
    private readonly Dictionary<long, UserState> _userStates = new();
    private readonly Dictionary<string, long> _callbackChatIds = new(); // Хранилище chatId для callback'ов
    private readonly Dictionary<long, long> _userChats = new(); // Хранилище последнего chatId для каждого пользователя

    // Список доступных команд для справки
    private readonly List<BotCommandInfo> _availableCommands = new()
    {
        new BotCommandInfo { Command = "/start", Description = "Начать работу с ботом" },
        new BotCommandInfo { Command = "/help", Description = "Показать справку" },
        new BotCommandInfo { Command = "/menu", Description = "Показать меню" },
        new BotCommandInfo { Command = "/weather", Description = "Узнать погоду" },
        new BotCommandInfo { Command = "/stop", Description = "Остановить бота" }
    };

    public CommandBot(IMaxBotClient botClient, ILogger<CommandBot> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        // Получаем информацию о боте для логирования
        var botInfo = await _botClient.GetMeAsync();
        _logger.LogInformation("Бот {BotName} запущен и готов к работе!", botInfo.FirstName);

        await _botClient.RunPollingAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync
        );
    }

    private async Task HandleUpdateAsync(Update update, IMaxBotClient botClient)
    {
        try
        {
            // Обработка новых сообщений
            if (update.UpdateType == UpdateTypes.MessageCreated && update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
            // Обработка callback-ов от кнопок
            else if (update.UpdateType == UpdateTypes.MessageCallback && update.Callback != null)
            {
                await HandleCallbackAsync(update.Callback);
            }
            // Обработка события запуска бота пользователем
            else if (update.UpdateType == UpdateTypes.BotStarted && update.UserId.HasValue)
            {
                await HandleBotStarted(update.UserId.Value, update.ChatId ?? 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления типа {UpdateType}", update.UpdateType);
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Recipient?.ChatId ?? 0;
        var userId = message.Sender?.Id ?? 0;
        var text = message.Body?.Text?.Trim() ?? "";

        if (chatId == 0)
        {
            _logger.LogWarning("Получено сообщение с chatId = 0 от пользователя {UserId}", userId);
            return;
        }

        // Сохраняем chatId для пользователя
        _userChats[userId] = chatId;

        _logger.LogDebug("Получено сообщение от {UserId} в чат {ChatId}: {Text}", userId, chatId, text);

        // Проверяем состояние пользователя (ожидание ввода города)
        if (_userStates.TryGetValue(userId, out var state) && state.CurrentCommand == "awaiting_city")
        {
            await HandleCityInput(chatId, userId, text);
            return;
        }

        // Обрабатываем команды
        if (text.StartsWith("/"))
        {
            await HandleCommand(chatId, userId, text);
        }
        else
        {
            // Если это не команда и не ожидание ввода, показываем меню
            await ShowMainMenu(chatId);
        }
    }

    private async Task HandleCommand(long chatId, long userId, string command)
    {
        if (chatId == 0)
        {
            _logger.LogError("Попытка обработать команду с chatId = 0");
            return;
        }

        // Сохраняем chatId для пользователя
        _userChats[userId] = chatId;

        // Приводим команду к нижнему регистру и удаляем лишние пробелы
        command = command.ToLower().Trim();

        switch (command)
        {
            case "/start":
                await SendWelcomeMessage(chatId, userId);
                break;

            case "/help":
                await SendHelpMessage(chatId);
                break;

            case "/menu":
                await ShowMainMenu(chatId);
                break;

            case "/weather":
                _userStates[userId] = new UserState { CurrentCommand = "awaiting_city" };
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    chatId,
                    "🌍 *Узнаем погоду*\n\nВведите название города:"
                ));
                break;

            case "/stop":
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    chatId,
                    "👋 *До свидания!*\n\nЧтобы начать заново, отправьте /start"
                ));
                // Очищаем состояние пользователя
                _userStates.Remove(userId);
                break;

            default:
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    chatId,
                    $"❌ *Неизвестная команда:* `{command}`\n\n" +
                    $"Отправьте /help для списка доступных команд."
                ));
                break;
        }
    }

    private async Task SendWelcomeMessage(long chatId, long userId)
    {
        if (chatId == 0) return;

        var welcomeMessage =
            $"👋 *Добро пожаловать!*\n\n" +
            $"Я бот-помощник. Рад видеть вас! 🎉\n\n" +
            $"📌 *Доступные команды:*\n" +
            $"{string.Join("\n", _availableCommands.Select(c => $"`{c.Command}` - {c.Description}"))}\n\n" +
            $"Используйте кнопки ниже для быстрой навигации:";

        var keyboard = CreateMainKeyboard();

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(chatId, welcomeMessage, keyboard)
        );
    }

    private async Task SendHelpMessage(long chatId)
    {
        if (chatId == 0) return;

        var helpText =
            "📚 *Справка по командам*\n\n" +
            string.Join("\n", _availableCommands.Select(c => $"• `{c.Command}` — {c.Description}")) +
            "\n\n✨ *Возможности бота:*\n" +
            "• Inline-кнопки для быстрой навигации\n" +
            "• Интерактивный ввод данных\n" +
            "• Информация о погоде\n\n" +
            "Нажмите /menu для открытия главного меню.";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, helpText));
    }

    private async Task ShowMainMenu(long chatId)
    {
        if (chatId == 0) return;

        var keyboard = CreateMainKeyboard();

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(
                chatId,
                "📋 *Главное меню*\n\nВыберите действие:",
                keyboard
            )
        );
    }

    private List<List<InlineKeyboardButton>> CreateMainKeyboard()
    {
        return new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "ℹ️ Информация",
                    Payload = "info",
                    Intent = ButtonIntent.Default
                },
                new InlineKeyboardButton
                {
                    Text = "🕒 Время",
                    Payload = "time",
                    Intent = ButtonIntent.Default
                }
            },
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "📅 Дата",
                    Payload = "date",
                    Intent = ButtonIntent.Default
                },
                new InlineKeyboardButton
                {
                    Text = "🎲 Случайное число",
                    Payload = "random",
                    Intent = ButtonIntent.Positive
                }
            },
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "🌦 Погода",
                    Payload = "weather",
                    Intent = ButtonIntent.Positive
                },
                new InlineKeyboardButton
                {
                    Text = "❓ Помощь",
                    Payload = "help",
                    Intent = ButtonIntent.Default
                }
            }
        };
    }

    private async Task HandleCallbackAsync(Callback callback)
    {
        var callbackId = callback.CallbackId;
        var userId = callback.User?.Id ?? 0;
        var payload = callback.Payload;

        _logger.LogDebug("Получен callback {CallbackId} от {UserId} с payload: {Payload}",
            callbackId, userId, payload);

        if (userId == 0)
        {
            _logger.LogError("Callback {CallbackId} не содержит userId", callbackId);
            return;
        }

        // Пробуем получить chatId из разных источников:
        // 1. Из самого callback'а
        long chatId = callback.ChatId ?? 0;

        // 2. Из сохраненного по callbackId
        if (chatId == 0 && _callbackChatIds.TryGetValue(callbackId, out var storedChatId))
        {
            chatId = storedChatId;
            _logger.LogDebug("Найден chatId {ChatId} для callback {CallbackId} в хранилище callback'ов", chatId, callbackId);
        }

        // 3. Из сохраненного по userId (последний известный чат пользователя)
        if (chatId == 0 && _userChats.TryGetValue(userId, out var userChatId))
        {
            chatId = userChatId;
            _logger.LogDebug("Используем последний известный chatId {ChatId} для пользователя {UserId}", chatId, userId);
        }

        // Если все еще нет chatId, логируем ошибку
        if (chatId == 0)
        {
            _logger.LogError("Не удалось определить chatId для callback {CallbackId} от пользователя {UserId}",
                callbackId, userId);

            // Пытаемся ответить на callback с ошибкой
            try
            {
                await _botClient.AnswerCallbackAsync(callbackId, new AnswerCallbackRequest
                {
                    Notification = "❌ Ошибка: не удалось определить чат"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить ответ на callback {CallbackId}", callbackId);
            }
            return;
        }

        // Сохраняем chatId для этого callbackId и пользователя
        _callbackChatIds[callbackId] = chatId;
        _userChats[userId] = chatId;

        // Отвечаем на callback с уведомлением
        try
        {
            var answerRequest = new AnswerCallbackRequest
            {
                Notification = "✅ Действие выполняется..."
            };

            await _botClient.AnswerCallbackAsync(callbackId, answerRequest);
            _logger.LogDebug("Отправлено подтверждение на callback {CallbackId}", callbackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить подтверждение на callback {CallbackId}", callbackId);
            // Продолжаем выполнение, так как основное действие все равно нужно обработать
        }

        // Обрабатываем само действие
        await ProcessCallbackAction(chatId, userId, payload);
    }

    private async Task ProcessCallbackAction(long chatId, long userId, string? payload)
    {
        if (chatId == 0)
        {
            _logger.LogError("Попытка обработать действие с chatId = 0");
            return;
        }

        try
        {
            switch (payload)
            {
                case "info":
                    await SendInfoMessage(chatId, userId);
                    break;

                case "time":
                    await SendTimeMessage(chatId);
                    break;

                case "date":
                    await SendDateMessage(chatId);
                    break;

                case "random":
                    await SendRandomNumberMessage(chatId);
                    break;

                case "weather":
                    _userStates[userId] = new UserState { CurrentCommand = "awaiting_city" };
                    await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                        chatId,
                        "🌍 *Узнаем погоду*\n\nВведите название города:"
                    ));
                    break;

                case "help":
                    await SendHelpMessage(chatId);
                    break;

                case "menu":
                    await ShowMainMenu(chatId);
                    break;

                default:
                    _logger.LogWarning("Неизвестный payload: {Payload} от пользователя {UserId}", payload, userId);
                    await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                        chatId,
                        "❌ Неизвестное действие"
                    ));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке действия {Payload} для чата {ChatId} от пользователя {UserId}",
                payload, chatId, userId);

            // Пытаемся отправить сообщение об ошибке пользователю
            try
            {
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    chatId,
                    "❌ Произошла ошибка при выполнении действия. Пожалуйста, попробуйте позже."
                ));
            }
            catch
            {
                // Игнорируем, если не удалось отправить сообщение об ошибке
            }
        }
    }

    private async Task SendInfoMessage(long chatId, long userId)
    {
        if (chatId == 0) return;

        var infoMessage =
            "ℹ️ *Информация о боте*\n\n" +
            $"**Версия:** 2.0.0\n" +
            $"**ID пользователя:** `{userId}`\n" +
            $"**ID чата:** `{chatId}`\n" +
            $"**Время на сервере:** {DateTime.Now:HH:mm:ss}\n" +
            $"**Дата:** {DateTime.Now:dd.MM.yyyy}\n\n" +
            "🛠 *Техническая информация:*\n" +
            $"• Платформа: MAX Bot API\n" +
            $"• Библиотека: Kub.Max.Bot\n" +
            $"• Режим: Long Polling";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, infoMessage));
    }

    private async Task SendTimeMessage(long chatId)
    {
        if (chatId == 0) return;

        var currentTime = DateTime.Now;
        var timeMessage =
            $"🕒 *Текущее время*\n\n" +
            $"**Часы:** {currentTime:HH}\n" +
            $"**Минуты:** {currentTime:mm}\n" +
            $"**Секунды:** {currentTime:ss}\n\n" +
            $"**Полное время:** {currentTime:HH:mm:ss}\n" +
            $"**Часовой пояс:** {TimeZoneInfo.Local.DisplayName}";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, timeMessage));
    }

    private async Task SendDateMessage(long chatId)
    {
        if (chatId == 0) return;

        var currentDate = DateTime.Now;
        var dateMessage =
            $"📅 *Текущая дата*\n\n" +
            $"**День:** {currentDate:dd}\n" +
            $"**Месяц:** {currentDate:MMMM}\n" +
            $"**Год:** {currentDate:yyyy}\n\n" +
            $"**День недели:** {currentDate:dddd}\n" +
            $"**Полная дата:** {currentDate:dd MMMM yyyy} года\n" +
            $"**День в году:** {currentDate.DayOfYear}";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, dateMessage));
    }

    private async Task SendRandomNumberMessage(long chatId)
    {
        if (chatId == 0) return;

        var random = new Random();
        var number = random.Next(1, 1001);
        var isEven = number % 2 == 0;

        var randomMessage =
            $"🎲 *Случайное число*\n\n" +
            $"**Сгенерировано:** {number}\n" +
            $"**Свойства:** {(isEven ? "Чётное" : "Нечётное")}\n" +
            $"**Диапазон:** от 1 до 1000\n\n" +
            $"✨ Новое число каждый раз!";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, randomMessage));
    }

    private async Task HandleCityInput(long chatId, long userId, string city)
    {
        if (chatId == 0) return;

        // База данных городов с погодой
        var weatherData = new Dictionary<string, (int temp, string condition, string emoji)>
        {
            ["москва"] = (-5, "снег", "❄️"),
            ["спб"] = (-8, "облачно", "☁️"),
            ["казань"] = (-10, "ясно", "☀️"),
            ["екатеринбург"] = (-15, "снег", "❄️"),
            ["новосибирск"] = (-20, "морозно", "🥶"),
            ["сочи"] = (8, "дождливо", "🌧"),
            ["владивосток"] = (-12, "ветрено", "💨"),
            ["краснодар"] = (2, "пасмурно", "☁️"),
            ["ростов"] = (0, "облачно", "☁️"),
            ["самара"] = (-10, "снег", "❄️")
        };

        var normalizedCity = city.ToLower().Trim();

        if (weatherData.TryGetValue(normalizedCity, out var weather))
        {
            var random = new Random();
            var windSpeed = random.Next(2, 12);
            var humidity = random.Next(45, 95);

            // Красивое форматирование названия города
            var cityDisplay = char.ToUpper(normalizedCity[0]) + normalizedCity[1..];

            var weatherMessage =
                $"{weather.emoji} *Погода в городе {cityDisplay}*\n\n" +
                $"**🌡 Температура:** {weather.temp}°C\n" +
                $"**☁️ Состояние:** {weather.condition}\n" +
                $"**💨 Ветер:** {windSpeed} м/с\n" +
                $"**💧 Влажность:** {humidity}%\n" +
                $"**📊 Давление:** {random.Next(740, 780)} мм рт. ст.\n\n" +
                $"🕒 Обновлено: {DateTime.Now:HH:mm}";

            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, weatherMessage));
        }
        else
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                chatId,
                $"❌ *Город '{city}' не найден*\n\n" +
                "Попробуйте один из доступных городов:\n" +
                "• Москва\n" +
                "• СПб\n" +
                "• Казань\n" +
                "• Екатеринбург\n" +
                "• Новосибирск\n" +
                "• Сочи\n" +
                "• Владивосток\n" +
                "• Краснодар\n" +
                "• Ростов\n" +
                "• Самара\n\n" +
                "Или отправьте /menu для возврата в меню."
            ));
        }

        // Сбрасываем состояние
        _userStates.Remove(userId);
    }

    private async Task HandleBotStarted(long userId, long chatId)
    {
        if (chatId == 0)
        {
            _logger.LogWarning("Получено событие BotStarted с chatId = 0 для пользователя {UserId}", userId);
            return;
        }

        _logger.LogInformation("Пользователь {UserId} запустил бота в чате {ChatId}", userId, chatId);

        // Сохраняем chatId для пользователя
        _userChats[userId] = chatId;

        // Отправляем приветственное сообщение
        await SendWelcomeMessage(chatId, userId);
    }

    private Task HandleErrorAsync(Exception exception, Update? update)
    {
        _logger.LogError(exception, "Ошибка при обработке обновления. UpdateType: {UpdateType}",
            update?.UpdateType ?? "unknown");

        return Task.CompletedTask;
    }

    private class UserState
    {
        public string CurrentCommand { get; set; } = "";
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }

    private class BotCommandInfo
    {
        public string Command { get; set; } = "";
        public string Description { get; set; } = "";
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            var bot = host.Services.GetRequiredService<CommandBot>();

            Console.WriteLine("🚀 Запуск бота...");
            await bot.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Регистрируем клиента бота
                services.AddMaxBotClient(options =>
                {
                    options.Token = "YOUR_BOT_TOKEN"; // Замените на свой токен

                    options.Timeout = TimeSpan.FromSeconds(60);
                });

                services.AddSingleton<CommandBot>();

                services.AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .UseConsoleLifetime();
}