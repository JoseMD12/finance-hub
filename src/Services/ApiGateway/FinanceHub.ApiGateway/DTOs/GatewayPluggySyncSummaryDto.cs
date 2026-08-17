namespace FinanceHub.ApiGateway.DTOs;

public record GatewayPluggySyncSummaryDto(
    int TotalItemsSynced,
    int TotalAccountsSynced,
    int TotalCheckingTransactionsIngested,
    int TotalCardTransactionsIngested,
    DateTime SyncedAtUtc
);
