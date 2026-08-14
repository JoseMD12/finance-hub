using System;

namespace FinanceHub.Shared.Messaging.Exceptions;

public class JwtInfrastructureException : InfrastructureException
{
    public JwtInfrastructureException(string message, Exception? innerException = null)
        : base(message, "INFRA_JWT_ERROR", 500, innerException)
    {
    }
}
