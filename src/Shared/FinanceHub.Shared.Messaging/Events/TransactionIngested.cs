namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted by Bank Integration Services (Itaú, Mercado Pago, Inter) when a raw transaction is ingested.
/// </summary>
public record TransactionIngested(
    Guid IngestionId,
    string UserId,
    string Source, // "itau", "mercadopago", "inter"
    string AccountId,
    string? BankTransactionId,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    string Currency,
    string? RawPayloadJson,
    DateTime OccurredAtUtc
) : IFinanceHubEvent;
