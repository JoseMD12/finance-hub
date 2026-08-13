using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface IUserCategoryRuleRepository
{
    Task<UserCategoryRule?> FindByPatternAsync(string userId, string cleanPattern, CancellationToken cancellationToken);
    Task AddOrUpdateAsync(UserCategoryRule rule, CancellationToken cancellationToken);
}
