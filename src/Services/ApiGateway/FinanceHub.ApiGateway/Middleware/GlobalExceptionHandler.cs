using System.Diagnostics;

using FinanceHub.Shared.Observability.Exceptions.Mapping;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.ApiGateway.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IExceptionMapperRegistry _mapperRegistry;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IExceptionMapperRegistry mapperRegistry, ILogger<GlobalExceptionHandler> logger)
    {
        _mapperRegistry = mapperRegistry;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var problemDetails = _mapperRegistry.MapToProblemDetails(exception, httpContext, traceId);
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        var errorCode = problemDetails.Extensions["errorCode"]?.ToString() ?? "UNKNOWN_ERROR";

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Erro grave capturado no GlobalExceptionHandler [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }
        else
        {
            _logger.LogWarning("Falha tratada no GlobalExceptionHandler [TraceId: {TraceId}, ErrorCode: {ErrorCode}]: {Message}", traceId, errorCode, exception.Message);
        }

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
