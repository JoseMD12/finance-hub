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

        var problem = new ProblemDetails
        {
            Status = infraEx.StatusCode,
            Title = "Erro de Infraestrutura",
            Detail = infraEx.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["errorCode"] = infraEx.ErrorCode;
        problem.Extensions["traceId"] = traceId;

        return problem;
    }
}
