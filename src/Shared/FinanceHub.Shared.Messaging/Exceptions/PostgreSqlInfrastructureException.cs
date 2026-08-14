using System;

namespace FinanceHub.Shared.Messaging.Exceptions;

public class PostgreSqlInfrastructureException : InfrastructureException
{
    public PostgreSqlInfrastructureException(string message, Exception? innerException = null)
        : base(message, "INFRA_POSTGRES_ERROR", 503, innerException)
    {
    }
}
