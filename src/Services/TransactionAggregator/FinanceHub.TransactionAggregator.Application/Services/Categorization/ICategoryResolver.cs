using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public record CategorizationResult(Guid CategoryId, CategorizationSource Source);

public interface ICategoryResolver
{
    int Priority { get; }
    Task<CategorizationResult?> ResolveAsync(string userId, SanitizedDescription description, CancellationToken cancellationToken);
}

public interface ICategoryResolverPipeline
{
    Task<CategorizationResult> ResolveCategoryAsync(string userId, string rawDescription, CancellationToken cancellationToken);
}
