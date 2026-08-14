using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public interface ICategoryResolverPipeline
{
    Task<CategorizationResult> ResolveCategoryAsync(string userId, string rawDescription, CancellationToken cancellationToken);
}
