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

    /// <summary>
    /// Natureza econômica declarada para a categoria. Devolve <c>Operating</c> quando a
    /// categoria não existe, para que a ingestão nunca falhe por catálogo incompleto.
    /// </summary>
    Task<TransactionNature> GetNatureByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
}
