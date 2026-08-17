namespace FinanceHub.PluggyIntegration.Domain.Constants;

public static class PluggyConstants
{
    public const string DefaultBaseUrl = "https://my-api.pluggy.ai";
    public const string ItemsEndpoint = "/items";
    public const string AccountsEndpoint = "/accounts";
    public const string TransactionsEndpoint = "/transactions";

    public static class Resilience
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MaxRetryAttempts = 3;
        public const int BaseRetryDelayMilliseconds = 500;
    }

    public static class AccountTypes
    {
        public const string Bank = "BANK";
        public const string Credit = "CREDIT";
    }

    public static class AccountSubtypes
    {
        public const string CheckingAccount = "CHECKING_ACCOUNT";
        public const string CreditCard = "CREDIT_CARD";
    }
}
