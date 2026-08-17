namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyCreditDataDto(
    decimal? AvailableCreditLimit,
    decimal? CreditLimit,
    string? BalanceDueDate
);

public record PluggyAccountDto(
    string Id,
    string Type,
    string Subtype,
    string Name,
    decimal Balance,
    string CurrencyCode,
    string ItemId,
    PluggyCreditDataDto? CreditData
);
