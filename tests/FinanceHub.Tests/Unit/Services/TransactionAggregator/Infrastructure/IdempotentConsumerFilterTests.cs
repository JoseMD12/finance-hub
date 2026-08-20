using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Infrastructure.Messaging.Filters;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Unit.Services.TransactionAggregator.Infrastructure;

[Trait("Category", "Unit")]
public class IdempotentConsumerFilterTests
{
    private readonly TransactionAggregatorDbContext _dbContext;

    public IdempotentConsumerFilterTests()
    {
        var options = new DbContextOptionsBuilder<TransactionAggregatorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TransactionAggregatorDbContext(options);
    }

    [Fact]
    public async Task Send_ShouldProceedAndSaveToInbox_WhenMessageHashIsNew()
    {
        // Arrange
        var logger = Substitute.For<ILogger<IdempotentConsumerFilter<TransactionsBatchIngested>>>();
        var filter = new IdempotentConsumerFilter<TransactionsBatchIngested>(_dbContext, logger);

        var batchId = Guid.NewGuid();
        var message = new TransactionsBatchIngested(
            BatchId: batchId,
            UserId: "user-123",
            ChunkIndex: 0,
            TotalChunks: 1,
            CheckingTransactions: [],
            CardTransactions: [],
            OccurredAtUtc: DateTime.UtcNow
        );

        var context = Substitute.For<ConsumeContext<TransactionsBatchIngested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        var next = Substitute.For<IPipe<ConsumeContext<TransactionsBatchIngested>>>();

        // Act
        await filter.Send(context, next);

        // Assert
        await next.Received(1).Send(context);
        var inboxEntry = await _dbContext.InboxProcessedMessages.FirstOrDefaultAsync(m => m.MessageHash == batchId.ToString());
        inboxEntry.Should().NotBeNull();
        inboxEntry!.EventType.Should().Be(nameof(TransactionsBatchIngested));
    }

    [Fact]
    public async Task Send_ShouldSkipHandler_WhenMessageHashAlreadyExistsInInbox()
    {
        // Arrange
        var existingHash = Guid.NewGuid().ToString();
        _dbContext.InboxProcessedMessages.Add(new InboxProcessedMessage(existingHash, nameof(TransactionsBatchIngested)));
        await _dbContext.SaveChangesAsync();

        var logger = Substitute.For<ILogger<IdempotentConsumerFilter<TransactionsBatchIngested>>>();
        var filter = new IdempotentConsumerFilter<TransactionsBatchIngested>(_dbContext, logger);

        var message = new TransactionsBatchIngested(
            BatchId: Guid.Parse(existingHash),
            UserId: "user-123",
            ChunkIndex: 0,
            TotalChunks: 1,
            CheckingTransactions: [],
            CardTransactions: [],
            OccurredAtUtc: DateTime.UtcNow
        );

        var context = Substitute.For<ConsumeContext<TransactionsBatchIngested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        var next = Substitute.For<IPipe<ConsumeContext<TransactionsBatchIngested>>>();

        // Act
        await filter.Send(context, next);

        // Assert
        await next.DidNotReceive().Send(Arg.Any<ConsumeContext<TransactionsBatchIngested>>());
    }
}
