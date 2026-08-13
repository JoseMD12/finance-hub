using System.Diagnostics;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.TransactionAggregator.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, title, errorCode) = exception switch
        {
            DomainException domainException => (
                domainException.StatusCode,
                domainException.Message,
                domainException.ErrorCode
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Ocorreu um erro interno no servidor ao processar a transação.",
                "INTERNAL_SERVER_ERROR"
            )
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Erro de servidor capturado pelo GlobalExceptionHandler: {Message} | TraceId: {TraceId}", exception.Message, traceId);
        }
        else
        {
            logger.LogWarning("Erro de dominio capturado pelo GlobalExceptionHandler: {ErrorCode} - {Message} | TraceId: {TraceId}", errorCode, exception.Message, traceId);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://financehub.api/errors/{errorCode.ToLowerInvariant()}",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["errorCode"] = errorCode;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
