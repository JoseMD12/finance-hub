using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface ITransactionRepository
{
    Task<bool> ExistsByHashAsync(TransactionHash hash, CancellationToken cancellationToken);
    Task<Guid?> GetIdByHashAsync(TransactionHash hash, CancellationToken cancellationToken);
    Task<CanonicalTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(CanonicalTransaction transaction, CancellationToken cancellationToken);
    Task UpdateAsync(CanonicalTransaction transaction, CancellationToken cancellationToken);
    Task<IEnumerable<TransactionDto>> GetProjectedByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IEnumerable<CanonicalTransaction>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
}
