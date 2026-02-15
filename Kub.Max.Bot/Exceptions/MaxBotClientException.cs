using System.Net;

namespace Kub.Max.Bot.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках в работе с MAX Bot API.
/// </summary>
public class MaxBotClientException : Exception
{
    /// <summary>
    /// HTTP статус код ответа.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Тело ответа, если доступно
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Создаёт новый экземпляр исключения.
    /// </summary>
    public MaxBotClientException(string message, HttpStatusCode statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Создаёт новый экземпляр исключения с внутренним исключением.
    /// </summary>
    public MaxBotClientException(string message, HttpStatusCode statusCode, Exception innerException, string? responseBody = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}";
    }
}