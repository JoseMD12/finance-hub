using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public interface IExceptionMapper
{
    int Priority { get; }
    bool CanMap(Exception exception);
    ProblemDetails Map(Exception exception, HttpContext context, string traceId);
}
