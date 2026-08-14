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

        return ProblemDetailsFactory.Create(
            status,
            title,
            exception.Message,
            errorCode,
            traceId,
            context.Request.Path);
    }
}
