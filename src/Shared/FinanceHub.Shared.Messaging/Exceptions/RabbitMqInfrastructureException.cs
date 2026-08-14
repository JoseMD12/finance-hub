using System;

namespace FinanceHub.Shared.Messaging.Exceptions;

public class RabbitMqInfrastructureException : InfrastructureException
{
    public RabbitMqInfrastructureException(string message, Exception? innerException = null)
        : base(message, "INFRA_RABBITMQ_ERROR", 503, innerException)
    {
    }
}
