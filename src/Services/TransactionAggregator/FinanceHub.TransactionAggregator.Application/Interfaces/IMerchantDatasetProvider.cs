using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public record MerchantDefinition(
    string Id,
    string Name,
    Guid CategoryId,
    IReadOnlyList<string> Patterns,
    string CleanName,
    int Priority = 100);

public interface IMerchantDatasetProvider
{
    MerchantDefinition? Match(string cleanText);
    IReadOnlyList<MerchantDefinition> GetAllMerchants();
}
