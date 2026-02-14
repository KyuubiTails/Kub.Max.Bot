using System.Net;

namespace Kub.Max.Bot.Exceptions;


/// Исключение, возникающее при ошибках в работе с MAX Bot API.

public class MaxBotClientException : Exception
{
    
    /// HTTP статус код ответа.
    
    public HttpStatusCode StatusCode { get; }

    
    /// Создаёт новый экземпляр исключения.
    
    public MaxBotClientException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    
    /// Создаёт новый экземпляр исключения с внутренним исключением.
    
    public MaxBotClientException(string message, HttpStatusCode statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}