using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Queries.GetDashboardSummary;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.TransactionAggregator.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard");

        group.MapGet("/summary", async (
            [AsParameters] GetDashboardSummaryParameters parameters,
            IGetDashboardSummaryQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(parameters.UserId))
            {
                return Results.Problem(
                    title: "Usuário não informado",
                    detail: "O parâmetro userId é obrigatório para montar o resumo do Dashboard.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var query = new GetDashboardSummaryQuery(
                parameters.UserId,
                parameters.StartDate,
                parameters.EndDate,
                parameters.InstitutionId,
                parameters.IncludeIgnoredInTotals ?? false);

            var result = await handler.Handle(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetDashboardSummary")
        .Produces<DashboardSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
