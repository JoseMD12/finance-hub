using System.Diagnostics;

using FinanceHub.ApiGateway.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.ApiGateway.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, title, errorCode) = exception switch
        {
            GatewayDomainException gatewayEx => ((int)gatewayEx.StatusCode, gatewayEx.Message, gatewayEx.ErrorCode),
            OperationCanceledException => (StatusCodes.Status504GatewayTimeout, "A requisição excedeu o tempo limite.", "REQUEST_TIMEOUT"),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no API Gateway.", "INTERNAL_SERVER_ERROR")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Erro grave no API Gateway [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }
        else
        {
            _logger.LogWarning("Falha tratada no API Gateway [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["errorCode"] = errorCode;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            type: typeof(ProblemDetails),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
