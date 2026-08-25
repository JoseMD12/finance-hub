using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Application;

public class GetTransactionsQueryHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly GetTransactionsQueryHandler _handler;

    public GetTransactionsQueryHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _handler = new GetTransactionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenTransactionsExist_ShouldReturnPagedResponse()
    {
        // Arrange
        var userId = "user-123";
        var filter = new TransactionFilterDto(userId, 1, 10);
        var query = new GetTransactionsQuery(filter);

        var projectedList = new List<TransactionDto>
        {
            new(Guid.NewGuid(), userId, "itau", "acc-1", 100.50m, "BRL", "Debit", "Supermercado", Guid.NewGuid(), "UserManual", true, DateTime.UtcNow, "DebitCard", "Supermercado"),
            new(Guid.NewGuid(), userId, "itau", "acc-1", 50.00m, "BRL", "Debit", "Farmacia", Guid.NewGuid(), "Rule", false, DateTime.UtcNow, "DebitCard", "Farmacia")
        };

        var expectedResponse = new PagedTransactionsResponseDto(
            projectedList,
            new TransactionSummaryDto(0m, 150.50m, -150.50m, 2),
            1,
            10,
            2,
            1);

        _repository.QueryPagedByFilterAsync(filter, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeEquivalentTo(projectedList);
        result.Summary.TotalExpense.Should().Be(150.50m);
        result.TotalItems.Should().Be(2);
        await _repository.Received(1).QueryPagedByFilterAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_ShouldReturnEmptyPagedResponse()
    {
        // Arrange
        var userId = "user-empty";
        var filter = new TransactionFilterDto(userId, 1, 20);
        var query = new GetTransactionsQuery(filter);

        var emptyResponse = new PagedTransactionsResponseDto(
            Enumerable.Empty<TransactionDto>(),
            new TransactionSummaryDto(0m, 0m, 0m, 0),
            1,
            20,
            0,
            0);

        _repository.QueryPagedByFilterAsync(filter, Arg.Any<CancellationToken>())
            .Returns(emptyResponse);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }
}
