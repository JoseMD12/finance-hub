using System.Diagnostics;
using FinanceHub.AuthConsent.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.AuthConsent.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is DomainException domainException)
        {
            logger.LogWarning(exception, "Exceção de domínio capturada. TraceId: {TraceId}, ErrorCode: {ErrorCode}",
                traceId, domainException.ErrorCode);

            var problemDetails = new ProblemDetails
            {
                Status = domainException.StatusCode,
                Title = "Regra de Domínio Violada",
                Detail = domainException.Message,
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["errorCode"] = domainException.ErrorCode,
                    ["traceId"] = traceId
                }
            };

            httpContext.Response.StatusCode = domainException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Exceção não tratada capturada. TraceId: {TraceId}", traceId);

        var unhandledProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro Interno no Servidor",
            Detail = "Ocorreu um erro inesperado ao processar a requisição.",
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = "INTERNAL_SERVER_ERROR",
                ["traceId"] = traceId
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(unhandledProblem, cancellationToken);
        return true;
    }
}
