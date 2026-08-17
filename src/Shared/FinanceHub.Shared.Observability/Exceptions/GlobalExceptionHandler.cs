using System.Diagnostics;
using System.Text.Json;
using FinanceHub.Shared.Observability.Exceptions.Mapping;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FinanceHub.Shared.Observability.Exceptions;

public sealed class GlobalExceptionHandler(
    IExceptionMapperRegistry mapperRegistry,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var problemDetails = mapperRegistry.MapToProblemDetails(exception, httpContext, traceId);
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        var errorCode = problemDetails.Extensions.TryGetValue("errorCode", out var codeObj) && codeObj != null
            ? codeObj.ToString() ?? "UNKNOWN_ERROR"
            : "UNKNOWN_ERROR";

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Erro de infraestrutura [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }
        else
        {
            logger.LogWarning("Regra de negócio tratada [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        
        var json = JsonSerializer.Serialize(problemDetails);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}
