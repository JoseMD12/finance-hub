using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface IAccountBalanceRepository
{
    Task<AccountBalance?> GetByUserAndAccountAsync(string userId, AccountIdentifier accountInfo, CancellationToken cancellationToken);
    Task<IEnumerable<AccountBalance>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task AddOrUpdateAsync(AccountBalance balance, CancellationToken cancellationToken);
}
