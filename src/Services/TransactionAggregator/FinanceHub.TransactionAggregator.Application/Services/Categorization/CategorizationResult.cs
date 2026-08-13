using System;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public record CategorizationResult(Guid CategoryId, CategorizationSource Source);
