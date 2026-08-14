using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public class DomainExceptionMapper : IExceptionMapper
{
    public int Priority => 20;

    public bool CanMap(Exception exception)
    {
        var type = exception.GetType();
        return type.Name.EndsWith("DomainException") ||
               type.GetProperty("ErrorCode") != null;
    }

    public ProblemDetails Map(Exception exception, HttpContext context, string traceId)
    {
        var type = exception.GetType();

        var statusCodeRaw = type.GetProperty("StatusCode")?.GetValue(exception);
        int statusCode;
        if (statusCodeRaw is int intStatus)
        {
            statusCode = intStatus;
        }
        else if (statusCodeRaw is HttpStatusCode httpStatus)
        {
            statusCode = (int)httpStatus;
        }
        else if (statusCodeRaw != null && Enum.TryParse<HttpStatusCode>(statusCodeRaw.ToString(), out var parsedStatus))
        {
            statusCode = (int)parsedStatus;
        }
        else
        {
            statusCode = StatusCodes.Status400BadRequest;
        }

        var errorCode = (string?)(type.GetProperty("ErrorCode")?.GetValue(exception)) ?? "DOMAIN_ERROR";

        return ProblemDetailsFactory.Create(
            statusCode,
            "Regra de Domínio Violada",
            exception.Message,
            errorCode,
            traceId,
            context.Request.Path);
    }
}
