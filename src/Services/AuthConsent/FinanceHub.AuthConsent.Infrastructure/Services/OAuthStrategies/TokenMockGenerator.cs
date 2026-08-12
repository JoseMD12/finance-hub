namespace FinanceHub.AuthConsent.Infrastructure.Services.OAuthStrategies;

public static class TokenMockGenerator
{
    public static string Generate(string bankPrefix, params string[] tokenActions)
    {
        var actionsCombined = string.Join('-', tokenActions);
        return $"{bankPrefix}-{actionsCombined}-{Guid.NewGuid():N}";
    }
}
