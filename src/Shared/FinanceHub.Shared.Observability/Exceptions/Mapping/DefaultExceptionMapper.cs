using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public class DefaultExceptionMapper : IExceptionMapper
{
    public int Priority => int.MaxValue;

    public bool CanMap(Exception exception) => true;

    public ProblemDetails Map(Exception exception, HttpContext context, string traceId)
    {
        var (status, title, errorCode) = exception switch
        {
            OperationCanceledException => (StatusCodes.Status504GatewayTimeout, "Tempo Limite Excedido", "REQUEST_TIMEOUT"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Acesso Não Autorizado", "UNAUTHORIZED"),
            _ => (StatusCodes.Status500InternalServerError, "Erro Interno no Servidor", "INTERNAL_SERVER_ERROR")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = traceId;

        return problem;
    }
}
