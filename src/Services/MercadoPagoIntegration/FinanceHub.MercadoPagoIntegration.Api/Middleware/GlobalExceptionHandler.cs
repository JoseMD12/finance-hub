using System.Diagnostics;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FinanceHub.MercadoPagoIntegration.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
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

        var statusCode = exception switch
        {
            DomainException de => de.StatusCode,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var errorCode = exception switch
        {
            DomainException de => de.ErrorCode,
            ArgumentException => "INVALID_ARGUMENT",
            _ => "INTERNAL_SERVER_ERROR"
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode >= 500 ? "Erro Interno do Servidor" : "Erro de Negócio",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
                ["errorCode"] = errorCode
            }
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Erro de sistema [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }
        else
        {
            _logger.LogWarning("Regra tratada [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
