using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.DTOs;

public record TransactionDto(
    Guid Id,
    string UserId,
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    string Type,
    string Description,
    Guid CategoryId,
    string CategorizationSource,
    bool IsManuallyCategorized,
    DateTime TransactionDateUtc,
    string Channel,
    string MerchantName,
    string Nature = "Operating",
    bool IsBillPayment = false,
    bool IsIgnoredInTotals = false,
    Guid? PairedTransactionId = null);

public record AccountBalanceDto(
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DateTime LastUpdatedAtUtc);

public record ConsolidatedBalanceDto(
    string UserId,
    decimal TotalBalanceBrl,
    IEnumerable<AccountBalanceDto> AccountBalances);
