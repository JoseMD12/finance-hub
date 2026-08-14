using System;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public record CategorizationResult(Guid CategoryId, CategorizationSource Source);
