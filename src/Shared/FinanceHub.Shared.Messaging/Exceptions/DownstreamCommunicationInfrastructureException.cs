using System;

namespace FinanceHub.Shared.Messaging.Exceptions;

public class DownstreamCommunicationInfrastructureException : InfrastructureException
{
    public string ServiceName { get; }

    public DownstreamCommunicationInfrastructureException(string serviceName, string message, Exception? innerException = null)
        : base(message, "INFRA_DOWNSTREAM_ERROR", 503, innerException)
    {
        ServiceName = serviceName;
    }
}
