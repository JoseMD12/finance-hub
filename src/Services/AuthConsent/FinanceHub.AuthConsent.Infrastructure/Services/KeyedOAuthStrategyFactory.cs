using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.AuthConsent.Infrastructure.Services;

public sealed class KeyedOAuthStrategyFactory(IServiceProvider serviceProvider) : IKeyedOAuthStrategyFactory
{
    public IOAuthBankClientStrategy GetStrategy(string institutionId)
    {
        var key = institutionId.ToLowerInvariant();
        var strategy = serviceProvider.GetKeyedService<IOAuthBankClientStrategy>(key);

        if (strategy is null)
            throw new OAuthBankCommunicationDomainException(key, $"Nenhum cliente OAuth2 configurado para a instituição '{key}'.");

        return strategy;
    }
}
