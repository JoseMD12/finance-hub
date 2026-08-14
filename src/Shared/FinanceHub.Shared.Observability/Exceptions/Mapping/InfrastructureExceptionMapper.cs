using System;
using FinanceHub.Shared.Messaging.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public class InfrastructureExceptionMapper : IExceptionMapper
{
    public int Priority => 10;

    public bool CanMap(Exception exception) => exception is InfrastructureException;

    public ProblemDetails Map(Exception exception, HttpContext context, string traceId)
    {
        var infraEx = (InfrastructureException)exception;

        return ProblemDetailsFactory.Create(
            infraEx.StatusCode,
            "Erro de Infraestrutura",
            infraEx.Message,
            infraEx.ErrorCode,
            traceId,
            context.Request.Path);
    }
}
