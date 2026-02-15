using Kub.Max.Bot;
using Kub.Max.Bot.Extensions;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Models;
using Kub.Max.Bot.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoBot;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await RunBotAsync(host.Services);
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
            });

    static async Task RunBotAsync(IServiceProvider services)
    {
        var botClient = services.GetRequiredService<IMaxBotClient>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Проверяем подключение
            var botInfo = await botClient.GetMeAsync();
            logger.LogInformation("Бот {Name} запущен и готов к работе!", botInfo.FirstName);

            // Запускаем Long Polling
            await botClient.RunPollingAsync(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                limit: 100,
                timeout: 30
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка при работе бота");
        }
    }

    static async Task HandleUpdateAsync(Update update, IMaxBotClient botClient)
    {
        // Обрабатываем только новые сообщения
        if (update.UpdateType != UpdateTypes.MessageCreated || update.Message == null)
            return;

        var message = update.Message;
        var chatId = message.Recipient?.ChatId ?? 0;
        var text = message.Body?.Text;

        if (string.IsNullOrEmpty(text))
            return;

        // Эхо-ответ
        var response = await botClient.SendMessageAsync(
            SendMessageRequest.CreateText(
                chatId: chatId,
                text: $"Вы написали: {text}"
            )
        );

        if (response.Success)
        {
            Console.WriteLine($"Ответ отправлен в чат {chatId}");
        }
    }

    static Task HandleErrorAsync(Exception exception, Update? update)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        if (update != null)
        {
            Console.WriteLine($"При обработке обновления типа: {update.UpdateType}");
        }
        return Task.CompletedTask;
    }
}