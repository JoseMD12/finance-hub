namespace FinanceHub.MercadoPagoIntegration.Domain.Constants;

public static class MercadoPagoConstants
{
    public const string BankIdentifier = "mercadopago";
    public const string InstitutionName = "Mercado Pago";
    public const string DefaultCurrency = "BRL";
    public const int DefaultInitialSyncDays = 90;
    public const int SafetyOverlapHours = 24;
    public const int DefaultPageSize = 100;

    public static class OpenFinanceEndpoints
    {
        public const string AuthApiKey = "auth";
        public const string ConnectToken = "connect_token";
        public const string Accounts = "accounts";
        public const string Transactions = "transactions";
        public const string Items = "items";
    }
}
