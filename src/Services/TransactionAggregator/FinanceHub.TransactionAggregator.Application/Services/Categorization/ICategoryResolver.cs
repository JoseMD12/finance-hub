using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public interface ICategoryResolver
{
    int Priority { get; }
    Task<CategorizationResult?> ResolveAsync(string userId, SanitizedDescription description, CancellationToken cancellationToken);
}
