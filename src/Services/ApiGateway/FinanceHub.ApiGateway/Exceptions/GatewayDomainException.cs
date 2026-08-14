using System.Net;

namespace FinanceHub.ApiGateway.Exceptions;

public abstract class GatewayDomainException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ErrorCode { get; }

    protected GatewayDomainException(string message, HttpStatusCode statusCode, string errorCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
