namespace FinanceHub.Shared.Connectors;

public interface IBankConnector
{
    string BankIdentifier { get; }

    Task<AuthTokenResponse> AuthenticateAsync(
        BankCredentials credentials,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankAccountDto>> GetAccountsAsync(
        AuthTokenResponse token,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsAsync(
        AuthTokenResponse token,
        string accountId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}

public record BankCredentials(
    string ClientId,
    string ClientSecret,
    string? Scopes = null,
    string? CertificateThumbprint = null
);

public record AuthTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds,
    string TokenType = "Bearer"
);

public record BankAccountDto(
    string AccountId,
    string BankIdentifier,
    string AccountType,
    string Currency = "BRL",
    string? Nickname = null
);

public record BankTransactionDto(
    string TransactionId,
    string AccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset BookingDateTime,
    string TransactionInformation,
    string CreditDebitIndicator, // "CRDT" or "DBIT"
    decimal? FeeAmount = null,
    string? RawPayload = null
);

public record HealthCheckResult(
    bool IsHealthy,
    string Message,
    TimeSpan ResponseTime
);
