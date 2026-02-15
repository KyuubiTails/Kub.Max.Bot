using Kub.Max.Bot;
using Kub.Max.Bot.Extensions;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DialogBot;

// Состояния пользователя
public enum UserState
{
    Idle,
    AwaitingName,
    AwaitingAge,
    AwaitingCity,
    AwaitingFeedback,
    AwaitingConfirm
}

// Данные пользователя
public class UserData
{
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public UserState State { get; set; } = UserState.Idle;
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? City { get; set; }
    public string? Feedback { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

class DialogBot
{
    private readonly IMaxBotClient _botClient;
    private readonly ILogger<DialogBot> _logger;
    private readonly ConcurrentDictionary<long, UserData> _users = new();
    private readonly ConcurrentDictionary<string, CallbackInfo> _callbackCache = new();

    // Список доступных команд
    private readonly List<(string Command, string Description)> _commands = new()
    {
        ("/start", "Начать диалог"),
        ("/reset", "Сбросить данные"),
        ("/profile", "Показать профиль"),
        ("/feedback", "Оставить отзыв"),
        ("/menu", "Главное меню"),
        ("/help", "Показать справку")
    };

    public DialogBot(IMaxBotClient botClient, ILogger<DialogBot> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        var botInfo = await _botClient.GetMeAsync();
        _logger.LogInformation("Диалоговый бот {BotName} запущен", botInfo.FirstName);

        // Запускаем фоновую задачу для очистки старых сессий
        _ = CleanupOldSessionsAsync();

        await _botClient.RunPollingAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync
        );
    }

    private async Task HandleUpdateAsync(Update update, IMaxBotClient botClient)
    {
        try
        {
            // Логируем полученное обновление для отладки
            _logger.LogDebug("Получено обновление: Type={UpdateType}, UserId={UserId}, ChatId={ChatId}, CallbackId={CallbackId}",
                update.UpdateType, update.UserId, update.ChatId, update.Callback?.CallbackId);

            if (update.UpdateType == UpdateTypes.MessageCreated && update.Message != null)
            {
                await HandleMessageAsync(update.Message);
            }
            else if (update.UpdateType == UpdateTypes.MessageCallback && update.Callback != null)
            {
                await HandleCallbackAsync(update, update.Callback);
            }
            else if (update.UpdateType == UpdateTypes.BotStarted)
            {
                await HandleBotStarted(update);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления типа {UpdateType}", update.UpdateType);
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var userId = message.Sender?.Id ?? 0;
        var chatId = message.Recipient?.ChatId ?? 0;
        var text = message.Body?.Text?.Trim() ?? "";

        if (chatId == 0 || userId == 0)
        {
            _logger.LogWarning("Получено сообщение с некорректными данными: ChatId={ChatId}, UserId={UserId}", chatId, userId);
            return;
        }

        _logger.LogDebug("Сообщение от {UserId} в чат {ChatId}: {Text}", userId, chatId, text);

        // Получаем или создаем данные пользователя
        var userData = _users.GetOrAdd(userId, new UserData
        {
            UserId = userId,
            ChatId = chatId
        });

        userData.LastActivity = DateTime.UtcNow;
        userData.ChatId = chatId; // Обновляем chatId на случай, если он изменился

        // Обрабатываем команды в любом состоянии
        if (text.StartsWith("/"))
        {
            await HandleCommand(userData, text);
            return;
        }

        // Обрабатываем в зависимости от состояния
        switch (userData.State)
        {
            case UserState.AwaitingName:
                await HandleNameInput(userData, text);
                break;

            case UserState.AwaitingAge:
                await HandleAgeInput(userData, text);
                break;

            case UserState.AwaitingCity:
                await HandleCityInput(userData, text);
                break;

            case UserState.AwaitingFeedback:
                await HandleFeedbackInput(userData, text);
                break;

            case UserState.AwaitingConfirm:
                await HandleConfirmInput(userData, text);
                break;

            default:
                await ShowMainMenu(userData);
                break;
        }
    }

    private async Task HandleCallbackAsync(Update update, Callback callback)
    {
        // Получаем userId из callback (он есть!)
        long? userId = callback.User?.Id;

        // Пробуем получить chatId из разных источников
        long? chatId = callback.ChatId ?? update.ChatId;

        if (userId == null)
        {
            _logger.LogError("Callback без userId");

            try
            {
                await _botClient.AnswerCallbackAsync(callback.CallbackId, new AnswerCallbackRequest
                {
                    Notification = "❌ Ошибка: не удалось определить пользователя"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить ответ на callback");
            }
            return;
        }

        // Если нет chatId, пытаемся получить из хранилища пользователей
        if (chatId == null || chatId == 0)
        {
            if (_users.TryGetValue(userId.Value, out var userData))
            {
                chatId = userData.ChatId;
                _logger.LogDebug("Использован chatId {ChatId} из хранилища для пользователя {UserId}", chatId, userId);
            }
        }

        // Если все еще нет chatId, логируем ошибку
        if (chatId == null || chatId == 0)
        {
            _logger.LogError("Не удалось определить chatId для пользователя {UserId}", userId);

            try
            {
                await _botClient.AnswerCallbackAsync(callback.CallbackId, new AnswerCallbackRequest
                {
                    Notification = "❌ Ошибка: не удалось определить чат"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить ответ на callback");
            }
            return;
        }

        // Сохраняем в кеш для будущих запросов
        _callbackCache[callback.CallbackId] = new CallbackInfo
        {
            UserId = userId.Value,
            ChatId = chatId.Value,
            Timestamp = DateTime.UtcNow
        };

        // Получаем данные пользователя
        if (!_users.TryGetValue(userId.Value, out var userDataForCallback))
        {
            userDataForCallback = new UserData
            {
                UserId = userId.Value,
                ChatId = chatId.Value
            };
            _users[userId.Value] = userDataForCallback;
        }

        // Отвечаем на callback с уведомлением
        await _botClient.AnswerCallbackAsync(callback.CallbackId, new AnswerCallbackRequest
        {
            Notification = "✅ Выполняю..."
        });

        _logger.LogDebug("Обработка callback {CallbackId} от пользователя {UserId} с payload: {Payload}",
            callback.CallbackId, userId, callback.Payload);

        switch (callback.Payload)
        {
            case "start_registration":
                await StartRegistration(userDataForCallback);
                break;

            case "show_profile":
                await ShowProfile(userDataForCallback);
                break;

            case "start_feedback":
                await StartFeedback(userDataForCallback);
                break;

            case "reset_data":
                await ResetUserData(userDataForCallback);
                break;

            case "confirm_yes":
                await CompleteRegistration(userDataForCallback);
                break;

            case "confirm_no":
                userDataForCallback.State = UserState.Idle;
                await StartRegistration(userDataForCallback);
                break;

            case "main_menu":
                await ShowMainMenu(userDataForCallback);
                break;

            case "show_help":
                await ShowHelp(userDataForCallback.ChatId);
                break;
        }
    }

    private async Task HandleCommand(UserData userData, string command)
    {
        switch (command.ToLower())
        {
            case "/start":
                await StartRegistration(userData);
                break;

            case "/reset":
                await ResetUserData(userData);
                break;

            case "/profile":
                await ShowProfile(userData);
                break;

            case "/feedback":
                await StartFeedback(userData);
                break;

            case "/menu":
                await ShowMainMenu(userData);
                break;

            case "/help":
                await ShowHelp(userData.ChatId);
                break;

            default:
                await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                    userData.ChatId,
                    $"❌ Неизвестная команда: {command}\n\nОтправьте /help для списка команд."
                ));
                break;
        }
    }

    private async Task ShowHelp(long chatId)
    {
        var helpText = "📚 *Доступные команды*\n\n";
        foreach (var cmd in _commands)
        {
            helpText += $"• `{cmd.Command}` — {cmd.Description}\n";
        }
        helpText += "\nТакже используйте кнопки меню для навигации.";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(chatId, helpText));
    }

    private async Task StartRegistration(UserData userData)
    {
        userData.State = UserState.AwaitingName;

        var welcomeMessage =
            "👋 *Добро пожаловать в регистрацию!*\n\n" +
            "Давайте познакомимся.\n\n" +
            "✏️ *Как вас зовут?*";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            welcomeMessage
        ));
    }

    private async Task HandleNameInput(UserData userData, string name)
    {
        if (name.Length < 2)
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                userData.ChatId,
                "❌ Имя должно содержать минимум 2 символа. Попробуйте еще раз:"
            ));
            return;
        }

        userData.Name = name;
        userData.State = UserState.AwaitingAge;

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            $"👋 Приятно познакомиться, {name}!\n\n📅 *Сколько вам лет?*"
        ));
    }

    private async Task HandleAgeInput(UserData userData, string ageStr)
    {
        if (!int.TryParse(ageStr, out int age) || age < 1 || age > 120)
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                userData.ChatId,
                "❌ Пожалуйста, введите корректный возраст (от 1 до 120):"
            ));
            return;
        }

        userData.Age = age;
        userData.State = UserState.AwaitingCity;

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            "🌍 *Из какого вы города?*"
        ));
    }

    private async Task HandleCityInput(UserData userData, string city)
    {
        if (city.Length < 2)
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                userData.ChatId,
                "❌ Название города должно содержать минимум 2 символа. Попробуйте еще раз:"
            ));
            return;
        }

        userData.City = city;
        userData.State = UserState.AwaitingConfirm;

        // Показываем сводку и просим подтверждение
        var summary =
            "📋 *Проверьте введенные данные:*\n\n" +
            $"👤 **Имя:** {userData.Name}\n" +
            $"📅 **Возраст:** {userData.Age}\n" +
            $"🌍 **Город:** {userData.City}\n\n" +
            "✅ Всё верно?";

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "✅ Да, всё верно",
                    Payload = "confirm_yes",
                    Intent = ButtonIntent.Positive
                },
                new InlineKeyboardButton
                {
                    Text = "🔄 Заполнить заново",
                    Payload = "confirm_no",
                    Intent = ButtonIntent.Negative
                }
            }
        };

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(userData.ChatId, summary, keyboard)
        );
    }

    private async Task HandleConfirmInput(UserData userData, string input)
    {
        if (input.ToLower() == "да" || input.ToLower() == "yes")
        {
            await CompleteRegistration(userData);
        }
        else
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                userData.ChatId,
                "🔄 Давайте начнем заново. Отправьте /start"
            ));
            userData.State = UserState.Idle;
        }
    }

    private async Task CompleteRegistration(UserData userData)
    {
        userData.State = UserState.Idle;

        var completionMessage =
            "🎉 *Регистрация завершена!*\n\n" +
            "Спасибо за предоставленную информацию!\n\n" +
            "📊 *Ваши данные сохранены:*\n" +
            $"👤 **Имя:** {userData.Name}\n" +
            $"📅 **Возраст:** {userData.Age}\n" +
            $"🌍 **Город:** {userData.City}\n\n" +
            "Теперь вы можете использовать команды:\n" +
            "/profile - посмотреть профиль\n" +
            "/feedback - оставить отзыв\n" +
            "/reset - сбросить данные\n" +
            "/menu - открыть меню";

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            completionMessage
        ));

        // Показываем меню
        await ShowMainMenu(userData);
    }

    private async Task StartFeedback(UserData userData)
    {
        userData.State = UserState.AwaitingFeedback;

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            "💬 *Оставьте ваш отзыв*\n\n" +
            "Напишите, что вы думаете о нашем боте:"
        ));
    }

    private async Task HandleFeedbackInput(UserData userData, string feedback)
    {
        userData.Feedback = feedback;
        userData.State = UserState.Idle;

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            "✅ Спасибо за ваш отзыв!\n\n" +
            "Мы обязательно учтем ваше мнение."
        ));

        _logger.LogInformation(
            "Отзыв от пользователя {UserId}: {Feedback}",
            userData.UserId,
            feedback
        );

        // Возвращаемся в меню
        await ShowMainMenu(userData);
    }

    private async Task ShowProfile(UserData userData)
    {
        if (string.IsNullOrEmpty(userData.Name))
        {
            await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
                userData.ChatId,
                "❌ Профиль не заполнен. Отправьте /start для регистрации."
            ));
            return;
        }

        var profileMessage =
            "👤 *Ваш профиль*\n\n" +
            $"🆔 **ID:** `{userData.UserId}`\n" +
            $"👤 **Имя:** {userData.Name}\n" +
            $"📅 **Возраст:** {userData.Age}\n" +
            $"🌍 **Город:** {userData.City}\n" +
            $"🕒 **Последняя активность:** {userData.LastActivity:HH:mm dd.MM.yyyy}";

        if (!string.IsNullOrEmpty(userData.Feedback))
        {
            profileMessage += $"\n\n💬 **Ваш отзыв:** {userData.Feedback}";
        }

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "📝 Оставить отзыв",
                    Payload = "start_feedback",
                    Intent = ButtonIntent.Positive
                },
                new InlineKeyboardButton
                {
                    Text = "🏠 Меню",
                    Payload = "main_menu",
                    Intent = ButtonIntent.Default
                }
            }
        };

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(userData.ChatId, profileMessage, keyboard)
        );
    }

    private async Task ResetUserData(UserData userData)
    {
        userData.Name = null;
        userData.Age = null;
        userData.City = null;
        userData.Feedback = null;
        userData.State = UserState.Idle;

        await _botClient.SendMessageAsync(SendMessageRequest.CreateText(
            userData.ChatId,
            "🔄 *Данные сброшены*\n\n" +
            "Отправьте /start чтобы начать заново или /menu для возврата в меню."
        ));

        await ShowMainMenu(userData);
    }

    private async Task ShowMainMenu(UserData userData)
    {
        var menuMessage =
            "🏠 *Главное меню*\n\n" +
            "Выберите действие:";

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "📝 Заполнить анкету",
                    Payload = "start_registration",
                    Intent = ButtonIntent.Positive
                }
            },
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "👤 Мой профиль",
                    Payload = "show_profile"
                },
                new InlineKeyboardButton
                {
                    Text = "💬 Отзыв",
                    Payload = "start_feedback"
                }
            },
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton
                {
                    Text = "🔄 Сбросить данные",
                    Payload = "reset_data",
                    Intent = ButtonIntent.Negative
                },
                new InlineKeyboardButton
                {
                    Text = "❓ Помощь",
                    Payload = "show_help",
                    Intent = ButtonIntent.Default
                }
            }
        };

        await _botClient.SendMessageAsync(
            SendMessageRequest.CreateWithKeyboard(userData.ChatId, menuMessage, keyboard)
        );
    }

    private async Task HandleBotStarted(Update update)
    {
        if (update.UserId.HasValue && update.ChatId.HasValue)
        {
            _logger.LogInformation("Пользователь {UserId} запустил бота в чате {ChatId}",
                update.UserId, update.ChatId);

            var userData = _users.GetOrAdd(update.UserId.Value, new UserData
            {
                UserId = update.UserId.Value,
                ChatId = update.ChatId.Value
            });

            await StartRegistration(userData);
        }
    }

    private async Task CleanupOldSessionsAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1));

                var cutoff = DateTime.UtcNow.AddHours(-24);

                // Очищаем старые сессии пользователей
                var oldSessions = _users.Where(x => x.Value.LastActivity < cutoff).ToList();
                foreach (var session in oldSessions)
                {
                    _users.TryRemove(session.Key, out _);
                }

                // Очищаем старый кеш callback'ов (старше 1 часа)
                var oldCallbacks = _callbackCache.Where(x => x.Value.Timestamp < cutoff).ToList();
                foreach (var cb in oldCallbacks)
                {
                    _callbackCache.TryRemove(cb.Key, out _);
                }

                if (oldSessions.Any() || oldCallbacks.Any())
                {
                    _logger.LogInformation("Очищено {SessionCount} сессий и {CallbackCount} callback'ов",
                        oldSessions.Count, oldCallbacks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке сессий");
            }
        }
    }

    private Task HandleErrorAsync(Exception exception, Update? update)
    {
        _logger.LogError(exception, "Ошибка в диалоговом боте. UpdateType: {UpdateType}",
            update?.UpdateType ?? "unknown");
        return Task.CompletedTask;
    }

    private class CallbackInfo
    {
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            var bot = host.Services.GetRequiredService<DialogBot>();

            Console.WriteLine("🚀 Запуск диалогового бота...");
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
                services.AddMaxBotClient(options =>
                {
                    options.Token = "YOUR_BOT_TOKEN"; // Замените на свой токен
                    options.Timeout = TimeSpan.FromSeconds(60);
                });

                services.AddSingleton<DialogBot>();
                services.AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(LogLevel.Debug); // Включаем Debug для отладки
                });
            })
            .UseConsoleLifetime();
}