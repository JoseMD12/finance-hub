namespace FinanceHub.ApiGateway;

public static class GatewayConstants
{
    public static class Status
    {
        public const string Healthy = "Healthy";
        public const string Unhealthy = "Unhealthy";
        public const string Degraded = "Degraded";
    }

    public static class Auth
    {
        public const string JwtSecretKeyEnvVar = "JWT_SECRET_KEY";
        public const string JwtIssuerEnvVar = "JWT_ISSUER";
        public const string JwtAudienceEnvVar = "JWT_AUDIENCE";
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
        public const string AuthConsentServiceName = "AuthConsent";
        public const string TransactionAggregatorServiceName = "TransactionAggregator";
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
