using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Api;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;
using FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Api;

public class TransactionEndpointsTests
{
    [Fact]
    public async Task GetHealth_ShouldReturn200OK()
    {
        using var factory = new CustomWebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task IngestTransaction_WithValidPayload_ShouldReturn201Created()
    {
        var ingestHandler = Substitute.For<IIngestTransactionCommandHandler>();
        var generatedId = Guid.NewGuid();

        ingestHandler.Handle(Arg.Any<IngestTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(generatedId);

        using var factory = new CustomWebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        await factory.InitializeAsync();

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => ingestHandler);
            });
        }).CreateClient();

        try
        {
            var command = new IngestTransactionCommand(
                "user-77",
                "itau",
                "acc-1",
                "tx-123",
                100m,
                "BRL",
                TransactionType.Debit,
                "PAG*Mercado 12/08",
                DateTime.UtcNow,
                TransactionChannel.Pix,
                "Mercado");

            var response = await client.PostAsJsonAsync("/api/v1/transactions/ingest", command);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetConsolidatedBalance_ShouldReturn200OK_WithBalances()
    {
        var balanceHandler = Substitute.For<IGetConsolidatedBalanceQueryHandler>();
        var expectedDto = new ConsolidatedBalanceDto("user-77", 500m, new List<AccountBalanceDto>
        {
            new AccountBalanceDto("itau", "acc-1", 500m, "BRL", DateTime.UtcNow)
        });

        balanceHandler.Handle(Arg.Any<GetConsolidatedBalanceQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        using var factory = new CustomWebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        await factory.InitializeAsync();

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => balanceHandler);
            });
        }).CreateClient();

        try
        {
            var response = await client.GetAsync("/api/v1/transactions/balances/user/user-77");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ConsolidatedBalanceDto>();

            result.Should().NotBeNull();
            result!.TotalBalanceBrl.Should().Be(500m);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
