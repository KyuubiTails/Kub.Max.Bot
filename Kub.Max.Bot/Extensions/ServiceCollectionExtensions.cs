using Kub.Max.Bot;
using Kub.Max.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kub.Max.Bot.Extensions;

/// <summary>
/// Расширения для регистрации клиента MAX Bot в контейнере внедрения зависимостей.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет MaxBotClient как Scoped сервис.
    /// </summary>
    public static IServiceCollection AddMaxBotClient(
        this IServiceCollection services,
        string token,
        TimeSpan? timeout = null,
        string? baseUrl = null)
    {
        services.AddScoped<IMaxBotClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<MaxBotClient>>();

            if (timeout.HasValue)
            {
                return new MaxBotClient(token, timeout.Value, baseUrl, logger);
            }

            return new MaxBotClient(token, baseUrl, logger: logger);
        });

        return services;
    }

    /// <summary>
    /// Добавляет MaxBotClient как Singleton сервис.
    /// </summary>
    public static IServiceCollection AddMaxBotClientSingleton(
        this IServiceCollection services,
        string token,
        TimeSpan? timeout = null,
        string? baseUrl = null)
    {
        services.AddSingleton<IMaxBotClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<MaxBotClient>>();

            if (timeout.HasValue)
            {
                return new MaxBotClient(token, timeout.Value, baseUrl, logger);
            }

            return new MaxBotClient(token, baseUrl, logger: logger);
        });

        return services;
    }

    /// <summary>
    /// Добавляет MaxBotClient с настройками из конфигурации.
    /// </summary>
    public static IServiceCollection AddMaxBotClient(
        this IServiceCollection services,
        Action<MaxBotClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddScoped<IMaxBotClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MaxBotClientOptions>>().Value;
            var logger = serviceProvider.GetService<ILogger<MaxBotClient>>();

            return new MaxBotClient(
                options.Token,
                options.Timeout,
                options.BaseUrl,
                logger);
        });

        return services;
    }
    /// <summary>
    /// Добавляет MaxBotClient с HttpClientFactory
    /// </summary>
    public static IServiceCollection AddMaxBotClientWithHttpClientFactory(
    this IServiceCollection services,
    string token,
    Action<HttpClient>? configureClient = null)
    {
        services.AddHttpClient<IMaxBotClient, MaxBotClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://platform-api.max.ru");
            client.DefaultRequestHeaders.Add("Authorization", token);
            client.Timeout = TimeSpan.FromSeconds(30);
            configureClient?.Invoke(client);
        });

        return services;
    }
}


/// <summary>
/// Настройки клиента MAX Bot.
/// </summary>
public class MaxBotClientOptions
{
    public string Token { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://platform-api.max.ru";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}