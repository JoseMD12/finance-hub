using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}
