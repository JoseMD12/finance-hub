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
        public const string TransactionAggregatorBaseUrlEnvVar = "TRANSACTION_AGGREGATOR_BASE_URL";
        public const string PluggyIntegrationBaseUrlEnvVar = "PLUGGY_INTEGRATION_BASE_URL";
        public const string TransactionAggregatorServiceName = "TransactionAggregator";
        public const string PluggyIntegrationServiceName = "PluggyIntegration";
        public const int DefaultTimeoutSeconds = 10;
    }

    public static class RateLimiting
    {
        public const string AnonymousPolicy = "AnonymousPolicy";
        public const string AuthenticatedPolicy = "AuthenticatedPolicy";
        public const int AnonymousPermitLimit = 30;
        public const int AuthenticatedPermitLimit = 120;
    }

    public static class Cors
    {
        public const string PolicyName = "FrontendCorsPolicy";
        public const string AllowedOriginsEnvVar = "CORS_ALLOWED_ORIGINS";
        public static readonly string[] DefaultOrigins =
        [
            "http://localhost:5173",
            "http://localhost:3000",
            "http://127.0.0.1:5173",
            "http://localhost:4173"
        ];
    }
}
