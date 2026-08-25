using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetCategories;

public class GetCategoriesQueryHandler : IGetCategoriesQueryHandler
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);

        var dtoList = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            c.ParentCategoryId,
            c.IconKey,
            c.ColorToken,
            c.IsSystemDefault,
            c.IsActive
        )).ToList();

        // Estruturar hierarquia de categorias principais e subcategorias
        var parentCategories = dtoList.Where(c => c.ParentCategoryId == null).ToList();
        var result = new List<CategoryDto>();

        foreach (var parent in parentCategories)
        {
            var subs = dtoList.Where(c => c.ParentCategoryId == parent.Id).ToList();
            result.Add(parent with { Subcategories = subs });
        }

        return result;
    }
}
