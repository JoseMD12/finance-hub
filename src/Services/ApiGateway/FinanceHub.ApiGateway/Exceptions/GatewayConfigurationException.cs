using System;
using System.Net;

namespace FinanceHub.ApiGateway.Exceptions;

public class GatewayConfigurationException : GatewayDomainException
{
    public string ConfigurationKey { get; }

    public GatewayConfigurationException(string configurationKey)
        : base($"A configuração ou variável de ambiente '{configurationKey}' é obrigatória e não foi informada.", HttpStatusCode.InternalServerError, "MISSING_CONFIGURATION")
    {
        ConfigurationKey = configurationKey;
    }
}
