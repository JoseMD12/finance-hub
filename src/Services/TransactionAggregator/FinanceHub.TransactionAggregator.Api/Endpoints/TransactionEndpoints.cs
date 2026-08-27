using System;
using FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;
using FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.TransactionAggregator.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/transactions")
            .WithTags("Transactions");

        group.MapPost("/ingest", async (
            IngestTransactionCommand command,
            IIngestTransactionCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var id = await handler.Handle(command, cancellationToken);
            return Results.Created($"/api/v1/transactions/{id}", new { Id = id });
        })
        .WithName("IngestTransaction")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", async (
            [AsParameters] GetTransactionsParameters parameters,
            IGetTransactionsQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var filter = new TransactionFilterDto(
                parameters.UserId,
                parameters.Page ?? 1,
                parameters.PageSize ?? 20,
                parameters.StartDate,
                parameters.EndDate,
                parameters.InstitutionId,
                parameters.CategoryId,
                parameters.Type,
                parameters.Search,
                parameters.IncludeIgnoredInTotals ?? false,
                parameters.ChannelGroup);

            var query = new GetTransactionsQuery(filter);
            var result = await handler.Handle(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetTransactions")
        .Produces<PagedTransactionsResponseDto>(StatusCodes.Status200OK);

        group.MapGet("/balances/user/{userId}", async (
            string userId,
            IGetConsolidatedBalanceQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetConsolidatedBalanceQuery(userId);
            var result = await handler.Handle(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetConsolidatedBalance")
        .Produces(StatusCodes.Status200OK);

        group.MapPatch("/{id:guid}/categorize", async (
            Guid id,
            CategorizeTransactionRequest request,
            ICategorizeTransactionCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CategorizeTransactionCommand(
                id,
                request.UserId,
                request.NewCategoryId,
                request.CreateCustomRule,
                request.ApplyToPastTransactions);

            await handler.Handle(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("CategorizeTransaction")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/neutrality", async (
            Guid id,
            ToggleTransactionNeutralityRequest request,
            FinanceHub.TransactionAggregator.Application.Commands.ToggleTransactionNeutrality.IToggleTransactionNeutralityCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new FinanceHub.TransactionAggregator.Application.Commands.ToggleTransactionNeutrality.ToggleTransactionNeutralityCommand(
                id,
                request.UserId,
                request.IsIgnoredInTotals,
                request.Reason);

            await handler.Handle(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("ToggleTransactionNeutrality")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/bill-payment", async (
            Guid id,
            ToggleTransactionBillPaymentRequest request,
            FinanceHub.TransactionAggregator.Application.Commands.ToggleBillPayment.IToggleBillPaymentCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new FinanceHub.TransactionAggregator.Application.Commands.ToggleBillPayment.ToggleBillPaymentCommand(
                id,
                request.UserId,
                request.IsBillPayment);

            await handler.Handle(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("ToggleTransactionBillPayment")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/notes", async (
            Guid id,
            UpdateTransactionNotesRequest request,
            FinanceHub.TransactionAggregator.Application.Commands.UpdateTransactionNotes.IUpdateTransactionNotesCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new FinanceHub.TransactionAggregator.Application.Commands.UpdateTransactionNotes.UpdateTransactionNotesCommand(
                id,
                request.UserId,
                request.Notes);

            await handler.Handle(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("UpdateTransactionNotes")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}

public record CategorizeTransactionRequest(
    string UserId,
    Guid NewCategoryId,
    bool CreateCustomRule,
    bool ApplyToPastTransactions = false);

public record ToggleTransactionNeutralityRequest(
    string UserId,
    bool IsIgnoredInTotals,
    string? Reason = null);

public record ToggleTransactionBillPaymentRequest(
    string UserId,
    bool IsBillPayment);

public record UpdateTransactionNotesRequest(
    string UserId,
    string? Notes);
