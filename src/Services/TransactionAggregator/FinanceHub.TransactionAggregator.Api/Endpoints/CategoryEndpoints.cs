using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Queries.GetCategories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.TransactionAggregator.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/categories")
            .WithTags("Categories");

        group.MapGet("/", async (
            IGetCategoriesQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoriesQuery();
            var result = await handler.Handle(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCategories")
        .Produces<IEnumerable<CategoryDto>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
