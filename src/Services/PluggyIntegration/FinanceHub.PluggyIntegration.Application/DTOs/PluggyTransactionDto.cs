namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyTransactionDto(
    string Id,
    string Description,
    decimal Amount,
    string Date,
    string? Type,
    string? Category
);
