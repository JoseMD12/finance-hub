using System.Net;

namespace FinanceHub.ApiGateway.Exceptions;

public class GatewayDownstreamException : GatewayDomainException
{
    public string ServiceName { get; }

    public GatewayDownstreamException(string serviceName, string message, Exception? innerException = null)
        : base($"Erro de comunicação com o serviço downstream '{serviceName}': {message}", HttpStatusCode.BadGateway, "DOWNSTREAM_ERROR", innerException)
    {
        ServiceName = serviceName;
    }
}
