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
    public async Task Handle_WhenTransactionsExist_ShouldReturnProjectedDtos()
    {
        // Arrange
        var userId = "user-123";
        var page = 1;
        var pageSize = 10;
        var query = new GetTransactionsQuery(userId, page, pageSize);

        var projectedList = new List<TransactionDto>
        {
            new(Guid.NewGuid(), userId, "itau", "acc-1", 100.50m, "BRL", "Debit", "Supermercado", Guid.NewGuid(), "UserManual", true, DateTime.UtcNow, "DebitCard", "Supermercado"),
            new(Guid.NewGuid(), userId, "itau", "acc-1", 50.00m, "BRL", "Debit", "Farmacia", Guid.NewGuid(), "Rule", false, DateTime.UtcNow, "DebitCard", "Farmacia")
        };

        _repository.GetProjectedByUserIdAsync(userId, page, pageSize, Arg.Any<CancellationToken>())
            .Returns(projectedList);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(projectedList);
        await _repository.Received(1).GetProjectedByUserIdAsync(userId, page, pageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = "user-empty";
        var query = new GetTransactionsQuery(userId, 1, 20);

        _repository.GetProjectedByUserIdAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<TransactionDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
