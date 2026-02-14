using Kub.Max.Bot;
using Kub.Max.Bot.Interfaces;
using Kub.Max.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Kub.Max.Bot.Extensions;

/// Расширения для регистрации клиента MAX Bot в контейнере внедрения зависимостей.
public static class ServiceCollectionExtensions
{
    /// Добавляет MaxBotClient как Scoped сервис.
    public static IServiceCollection AddMaxBotClient(
        this IServiceCollection services,
        string token,
        TimeSpan? timeout = null)
    {
        services.AddScoped<IMaxBotClient>(_ => new MaxBotClient(token, timeout ?? TimeSpan.FromSeconds(30)));
        return services;
    }

    /// Добавляет MaxBotClient как Singleton сервис.
    public static IServiceCollection AddMaxBotClientSingleton(
        this IServiceCollection services,
        string token,
        TimeSpan? timeout = null)
    {
        services.AddSingleton<IMaxBotClient>(_ => new MaxBotClient(token, timeout ?? TimeSpan.FromSeconds(30)));
        return services;
    }

    /// Добавляет MaxBotClient с настройками из конфигурации.
    public static IServiceCollection AddMaxBotClient(
        this IServiceCollection services,
        Action<MaxBotClientOptions> configureOptions)
    {
        var options = new MaxBotClientOptions();
        configureOptions(options);

        services.AddScoped<IMaxBotClient>(_ => new MaxBotClient(options.Token, options.Timeout));
        return services;
    }
}

/// Настройки клиента MAX Bot.
public class MaxBotClientOptions
{
    public string Token { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://platform-api.max.ru";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}