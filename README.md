Быстрый старт
1. Базовая инициализация клиента
csharp
using Kub.Max.Bot;
using Kub.Max.Bot.Requests;

// Токен вашего бота
string botToken = "YOUR_BOT_TOKEN";

// Создание экземпляра клиента
var botClient = new MaxBotClient(botToken);

// Получение информации о себе
var me = await botClient.GetMeAsync();
Console.WriteLine($"Бот @{me.Username} успешно запущен!");
2. Отправка простого сообщения
csharp
long myChatId = 123456789; // ID чата или пользователя
var messageText = "Привет, мир!";

var request = SendMessageRequest.CreateText(myChatId, messageText);
var response = await botClient.SendMessageAsync(request);

if (response.Success)
{
    Console.WriteLine($"Сообщение отправлено. ID: {response.Message?.Body?.Mid}");
}
3. Использование Long Polling
csharp
var cts = new CancellationTokenSource();

await botClient.RunPollingAsync(
    updateHandler: async (update, client) =>
    {
        // Обработка входящего сообщения
        if (update.Message?.Body?.Text is { } text)
        {
            Console.WriteLine($"Получено сообщение: {text}");
            
            // Эхо-ответ
            var reply = SendMessageRequest.CreateText(
                update.Message.Recipient.ChatId, 
                $"Вы сказали: {text}"
            );
            await client.SendMessageAsync(reply);
        }
    },
    cancellationToken: cts.Token
);

// Для остановки polling'а:
// cts.Cancel();
4. Интеграция с Dependency Injection (ASP.NET Core)
В файле Program.cs:

csharp
using Kub.Max.Bot.Extensions;

// Простой способ
builder.Services.AddMaxBotClient("YOUR_BOT_TOKEN");

// Или с настройками
builder.Services.AddMaxBotClient(options =>
{
    options.Token = "YOUR_BOT_TOKEN";
    options.Timeout = TimeSpan.FromSeconds(60);
});
Затем используйте в любом сервисе или контроллере:

csharp
public class MyBotService
{
    private readonly IMaxBotClient _botClient;

    public MyBotService(IMaxBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task DoSomething()
    {
        var info = await _botClient.GetMeAsync();
        // ...
    }
