using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

using FinanceHub.PluggyIntegration.Application.Commands.SyncSinglePluggyItem;

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

        group.MapPost("/items/{itemId}/sync", async (
            string itemId,
            string? userId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            ISyncSinglePluggyItemCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var pluggyToken = httpContext.Request.Headers[PluggyConstants.HeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                throw new NullOrEmptyPluggyAccessTokenDomainException();
            }

            var resolvedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value
                              ?? userId;
            var summary = await handler.HandleAsync(
                new SyncSinglePluggyItemCommand(itemId, resolvedUserId, pluggyToken),
                cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("SyncPluggyItem")
        .WithSummary("Solicita uma nova sincronização para uma instituição específica via Meu.Pluggy.");

        group.MapPost("/sync", async (
            string? userId,
            HttpContext httpContext,
            ISyncAllPluggyAccountsCommandHandler handler,
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

            var command = new SyncAllPluggyAccountsCommand(userId, pluggyToken);
            var summary = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("SyncPluggyAccounts")
        .WithSummary("Dispara a sincronização de todas as contas e transações via Meu.Pluggy.");

        return app;
    }
}
