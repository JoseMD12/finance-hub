using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public interface IExceptionMapperRegistry
{
    ProblemDetails MapToProblemDetails(Exception exception, HttpContext context, string traceId);
}
