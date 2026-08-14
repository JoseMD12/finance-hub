using System.Net;

namespace FinanceHub.ApiGateway.Exceptions;

public class GatewayTimeoutException : GatewayDomainException
{
    public string ServiceName { get; }

    public GatewayTimeoutException(string serviceName, Exception? innerException = null)
        : base($"O serviço downstream '{serviceName}' não respondeu dentro do limite de tempo.", HttpStatusCode.GatewayTimeout, "DOWNSTREAM_TIMEOUT", innerException)
    {
        ServiceName = serviceName;
    }
}
