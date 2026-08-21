using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public record MerchantDefinition(
    string Id,
    string Name,
    Guid CategoryId,
    IReadOnlyList<string> Patterns,
    string CleanName);

public interface IMerchantDatasetProvider
{
    MerchantDefinition? Match(string cleanText);
    IReadOnlyList<MerchantDefinition> GetAllMerchants();
}
