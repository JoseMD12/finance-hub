using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetCategories;

public interface IGetCategoriesQueryHandler
{
    Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken);
}
