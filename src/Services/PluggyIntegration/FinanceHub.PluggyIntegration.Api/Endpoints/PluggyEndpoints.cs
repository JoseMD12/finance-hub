using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.PluggyIntegration.Api.Endpoints;

public static class PluggyEndpoints
{
    public static IEndpointRouteBuilder MapPluggyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pluggy")
            .WithTags("PluggyIntegration");

        group.MapGet("/items", async (
            HttpContext httpContext,
            IGetPluggyItemsQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var pluggyToken = httpContext.Request.Headers[PluggyConstants.HeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                throw new NullOrEmptyPluggyAccessTokenDomainException();
            }

            var query = new GetPluggyItemsQuery(pluggyToken);
            var items = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(items);
        })
        .WithName("GetPluggyItems")
        .WithSummary("Lista todas as conexões e instituições bancárias vinculadas no Meu.Pluggy.");

        group.MapGet("/accounts", async (
            HttpContext httpContext,
            IGetPluggyAccountsQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var pluggyToken = httpContext.Request.Headers[PluggyConstants.HeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                throw new NullOrEmptyPluggyAccessTokenDomainException();
            }

            var accounts = await handler.HandleAsync(
                new GetPluggyAccountsQuery(pluggyToken),
                cancellationToken);
            return Results.Ok(accounts);
        })
        .WithName("GetPluggyAccounts")
        .WithSummary("Lista as contas correntes e cartões vinculados às instituições do Meu.Pluggy.");

        group.MapPost("/sync", (
            string? userId,
            HttpContext httpContext,
            ISyncAllPluggyAccountsCommandHandler handler,
            ISyncJobStore jobStore,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UserIdRequiredDomainException();
            }

            var pluggyToken = httpContext.Request.Headers[PluggyConstants.HeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                throw new NullOrEmptyPluggyAccessTokenDomainException();
            }

            var jobId = Guid.NewGuid();
            var command = new SyncAllPluggyAccountsCommand(userId, pluggyToken);

            jobStore.CreateJob(jobId, userId);

            _ = Task.Run(async () =>
            {
                try
                {
                    var summary = await handler.HandleAsync(command, CancellationToken.None);
                    jobStore.SetCompleted(jobId, summary);
                }
                catch (Exception ex)
                {
                    jobStore.SetFailed(jobId, ex.Message);
                }
            });

            var response = new SyncJobAcceptedDto(
                JobId: jobId,
                Status: "Processing",
                Message: "Sincronização em lote iniciada com sucesso em segundo plano.",
                StartedAtUtc: DateTime.UtcNow
            );

            return Results.Accepted($"/api/v1/pluggy/sync/jobs/{jobId}", response);
        })
        .WithName("SyncPluggyAccounts")
        .WithSummary("Dispara a sincronização assíncrona (202 Accepted) de todas as contas e transações via Meu.Pluggy.");

        group.MapGet("/sync/jobs/{jobId:guid}", (
            Guid jobId,
            ISyncJobStore jobStore) =>
        {
            var job = jobStore.GetJob(jobId);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        })
        .WithName("GetPluggySyncJobStatus")
        .WithSummary("Consulta o status e o resultado de um job de sincronização assíncrono.");

        return app;
    }
}
