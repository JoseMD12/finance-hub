using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public class ExceptionMapperRegistry : IExceptionMapperRegistry
{
    private readonly List<IExceptionMapper> _mappers;

    public ExceptionMapperRegistry(IEnumerable<IExceptionMapper> mappers)
    {
        _mappers = mappers.OrderBy(m => m.Priority).ToList();
    }

    public ProblemDetails MapToProblemDetails(Exception exception, HttpContext context, string traceId)
    {
        var mapper = _mappers.FirstOrDefault(m => m.CanMap(exception))
                  ?? new DefaultExceptionMapper();

        return mapper.Map(exception, context, traceId);
    }
}
