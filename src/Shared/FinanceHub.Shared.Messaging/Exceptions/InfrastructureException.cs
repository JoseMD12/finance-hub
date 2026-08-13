using System;

namespace FinanceHub.Shared.Messaging.Exceptions;

public abstract class InfrastructureException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected InfrastructureException(string message, string errorCode = "INFRA_ERROR", int statusCode = 500, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
