using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Application;

public class GetConsolidatedBalanceQueryHandlerTests
{
    private readonly IAccountBalanceRepository _repository;
    private readonly GetConsolidatedBalanceQueryHandler _handler;

    public GetConsolidatedBalanceQueryHandlerTests()
    {
        _repository = Substitute.For<IAccountBalanceRepository>();
        _handler = new GetConsolidatedBalanceQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenMultipleAccountsInBrl_ShouldSumTotalCorrectly()
    {
        // Arrange
        var userId = "user-abc";
        var query = new GetConsolidatedBalanceQuery(userId);

        var balanceDtos = new List<AccountBalanceDto>
        {
            new("itau", "acc-101", 1250.50m, "BRL", DateTime.UtcNow),
            new("inter", "acc-202", 750.25m, "BRL", DateTime.UtcNow)
        };

        _repository.GetProjectedByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(balanceDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.TotalBalanceBrl.Should().Be(2000.75m);
        result.AccountBalances.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNonBrlCurrencyPresent_ShouldOnlySumBrlInTotal()
    {
        // Arrange
        var userId = "user-multi-curr";
        var query = new GetConsolidatedBalanceQuery(userId);

        var balanceDtos = new List<AccountBalanceDto>
        {
            new("itau", "acc-brl", 1000.00m, "BRL", DateTime.UtcNow),
            new("inter", "acc-usd", 500.00m, "USD", DateTime.UtcNow)
        };

        _repository.GetProjectedByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(balanceDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalBalanceBrl.Should().Be(1000.00m);
        result.AccountBalances.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenNoAccountsFound_ShouldReturnZeroTotalAndEmptyList()
    {
        // Arrange
        var userId = "user-empty";
        var query = new GetConsolidatedBalanceQuery(userId);

        _repository.GetProjectedByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<AccountBalanceDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.TotalBalanceBrl.Should().Be(0m);
        result.AccountBalances.Should().BeEmpty();
    }
}
