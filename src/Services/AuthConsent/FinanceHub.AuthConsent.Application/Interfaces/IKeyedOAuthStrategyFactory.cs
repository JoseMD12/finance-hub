namespace FinanceHub.AuthConsent.Application.Interfaces;

public interface IKeyedOAuthStrategyFactory
{
    IOAuthBankClientStrategy GetStrategy(string institutionId);
}
