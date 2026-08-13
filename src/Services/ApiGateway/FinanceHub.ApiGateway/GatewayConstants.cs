namespace FinanceHub.ApiGateway;

public static class GatewayConstants
{
    public static class Auth
    {
        public const string JwtSecretKeyEnvVar = "JWT_SECRET_KEY";
        public const string JwtIssuerEnvVar = "JWT_ISSUER";
        public const string JwtAudienceEnvVar = "JWT_AUDIENCE";
        public const string DefaultIssuer = "https://financehub.local";
        public const string DefaultAudience = "financehub-gateway";
    }

    public static class Scopes
    {
        public const string Read = "openfinance:read";
        public const string Write = "openfinance:write";
        public const string Admin = "openfinance:admin";
    }

    public static class Downstream
    {
        public const string AuthConsentBaseUrlEnvVar = "AUTH_CONSENT_BASE_URL";
        public const string TransactionAggregatorBaseUrlEnvVar = "TRANSACTION_AGGREGATOR_BASE_URL";
        public const string DefaultAuthConsentUrl = "http://localhost:5001";
        public const string DefaultTransactionAggregatorUrl = "http://localhost:5002";
        public const int DefaultTimeoutSeconds = 10;
    }

    public static class RateLimiting
    {
        public const string AnonymousPolicy = "AnonymousPolicy";
        public const string AuthenticatedPolicy = "AuthenticatedPolicy";
        public const int AnonymousPermitLimit = 30;
        public const int AuthenticatedPermitLimit = 120;
    }
}
