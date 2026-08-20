using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.PluggyIntegration;

public class SyncAllPluggyAccountsCommandHandlerTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly IPluggyAggregationService _aggregationService = Substitute.For<IPluggyAggregationService>();
    private readonly IPluggyTransactionMapper _transactionMapper = new PluggyTransactionMapper();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly SyncAllPluggyAccountsCommandHandler _handler;

    public SyncAllPluggyAccountsCommandHandlerTests()
    {
        _handler = new SyncAllPluggyAccountsCommandHandler(
            _pluggyClient,
            _aggregationService,
            _transactionMapper,
            _publishEndpoint,
            NullLogger<SyncAllPluggyAccountsCommandHandler>.Instance
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WhenPluggyAccessTokenNullOrEmpty_ShouldThrowNullOrEmptyPluggyAccessTokenDomainException(string? invalidToken)
    {
        // Arrange
        var command = new SyncAllPluggyAccountsCommand("user-01", invalidToken!);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullOrEmptyPluggyAccessTokenDomainException>()
            .WithMessage("*X-Pluggy-Access-Token*");
    }

    [Fact]
    public async Task HandleAsync_WhenItemsAndAccountsExist_ShouldPassTokenToClientAndReturnSummaryAndPublishBatchEvent()
    {
        // Arrange
        const string validToken = "mock-pluggy-valid-token-123";
        var items = new List<PluggyItemDto>
        {
            new("item-inter-1", "UPDATED", new(77, "Banco Inter")),
            new("item-itau-2", "UPDATED", new(341, "Banco Itaú"))
        };

        var allAccounts = new List<PluggyAccountDto>
        {
            new("acc-inter-checking", "BANK", "CHECKING_ACCOUNT", "Inter Conta", 97.60m, "BRL", "item-inter-1", null),
            new("acc-inter-card", "CREDIT", "CREDIT_CARD", "Inter Gold", 1711.19m, "BRL", "item-inter-1", new(3000, 5000, "2026-08-20")),
            new("acc-itau-checking", "BANK", "CHECKING_ACCOUNT", "Itaú Conta", 211.00m, "BRL", "item-itau-2", null)
        };

        var allTxs = new List<PluggyTransactionDto>
        {
            new("tx-1", "Transferência recebida - Fundatec", 97.60m, "2026-08-14T00:00:00Z", "CREDIT", "Transfer - PIX", "acc-inter-checking"),
            new("tx-2", "MCDONALDS", 40.00m, "2026-08-15T00:00:00Z", "DEBIT", "Eating out", "acc-inter-card")
        };

        _pluggyClient.GetItemsAsync(validToken, Arg.Any<CancellationToken>()).Returns(items);
        _aggregationService.FetchAllAccountsAsync(validToken, Arg.Any<CancellationToken>()).Returns(allAccounts);
        _aggregationService.FetchAllTransactionsAsync(validToken, Arg.Any<CancellationToken>()).Returns(allTxs);

        // Act
        var result = await _handler.HandleAsync(new SyncAllPluggyAccountsCommand("user-01", validToken), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsSynced.Should().Be(2);
        result.TotalAccountsSynced.Should().Be(3);
        result.TotalCheckingTransactionsIngested.Should().Be(1);
        result.TotalCardTransactionsIngested.Should().Be(1);

        // Verify MassTransit Published TransactionsBatchIngested Event
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TransactionsBatchIngested>(e =>
                e.UserId == "user-01" &&
                e.CheckingTransactions.Count == 1 &&
                e.CardTransactions.Count == 1 &&
                e.ChunkIndex == 1 &&
                e.TotalChunks == 1
            ),
            Arg.Any<CancellationToken>()
        );
    }
}
