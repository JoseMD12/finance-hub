using System.Net;

namespace FinanceHub.ApiGateway.Exceptions;

public class GatewayCircuitOpenException : GatewayDomainException
{
    public string ServiceName { get; }

    public GatewayCircuitOpenException(string serviceName, Exception? innerException = null)
        : base($"O circuito de comunicação com o serviço '{serviceName}' está temporariamente aberto devido a falhas recorrentes.", HttpStatusCode.ServiceUnavailable, "CIRCUIT_OPEN", innerException)
    {
        ServiceName = serviceName;
    }
}
