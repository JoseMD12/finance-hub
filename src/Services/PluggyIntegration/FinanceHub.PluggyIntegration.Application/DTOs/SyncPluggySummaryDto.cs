namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record SyncPluggySummaryDto(
    int TotalItemsSynced,
    int TotalAccountsSynced,
    int TotalCheckingTransactionsIngested,
    int TotalCardTransactionsIngested,
    DateTime SyncedAtUtc
);
