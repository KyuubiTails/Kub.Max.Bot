# MAX Bot API Client для .NET

Библиотека для взаимодействия с MAX Bot API на платформе .NET. Поддерживает все методы API, включая Long Polling.

## Установка

Через NuGet:
dotnet add package KODKB.Max.Bot

## Быстрый старт

using Max.Bot;

// Инициализация клиента
var client = new MaxBotClient("your_bot_token");

// Получение информации о боте
var botInfo = await client.GetMeAsync();

// Отправка сообщения
var response = await client.SendMessageAsync(new SendMessageRequest
{
    ChatId = 123456789,
    Text = "Привет, мир!"
});

// Запуск Long Polling
await client.RunPollingAsync(async (update, botClient) =>
{
    if (update.UpdateType == UpdateTypes.MessageCreated)
    {
        Console.WriteLine($"Получено сообщение: {update.Message?.Body?.Text}");
        await botClient.SendMessageAsync(SendMessageRequest.CreateText(
            update.Message!.Recipient!.ChatId, 
            "Сообщение получено!"));
    }
});