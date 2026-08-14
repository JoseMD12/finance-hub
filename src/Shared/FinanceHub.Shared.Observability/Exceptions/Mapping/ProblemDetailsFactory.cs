using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public static class ProblemDetailsFactory
{
    public static ProblemDetails Create(
        int statusCode,
        string title,
        string detail,
        string errorCode,
        string traceId,
        string? instance = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance
        };

        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = traceId;

        return problem;
    }
}
