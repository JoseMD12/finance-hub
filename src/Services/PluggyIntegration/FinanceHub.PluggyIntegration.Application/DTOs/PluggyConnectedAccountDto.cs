namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyConnectedAccountDto(
    string ItemId,
    string InstitutionName,
    string Name,
    string Type,
    string Subtype,
    decimal Balance,
    PluggyCreditDataDto? CreditData
);
