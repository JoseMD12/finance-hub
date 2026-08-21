using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Application;

public class IngestTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _txRepo = Substitute.For<ITransactionRepository>();
    private readonly IAccountBalanceRepository _balanceRepo = Substitute.For<IAccountBalanceRepository>();
    private readonly ICategoryResolverPipeline _pipeline = Substitute.For<ICategoryResolverPipeline>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IngestTransactionCommandHandler _handler;
    private readonly Guid _categoryId = Guid.NewGuid();

    public IngestTransactionCommandHandlerTests()
    {
        _pipeline
            .ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CategorizationResult(_categoryId, CategorizationSource.GlobalRule));

        _handler = new IngestTransactionCommandHandler(_txRepo, _balanceRepo, _pipeline, _eventPublisher, _unitOfWork);
    }

    private static IngestTransactionCommand BuildCommand(
        string userId = "user-1",
        string institutionId = "itau",
        string accountNumber = "acc-100",
        string bankTransactionId = "bank-tx-99",
        decimal amount = 150.75m,
        string currency = "BRL",
        TransactionType type = TransactionType.Debit,
        string rawDescription = "PAG*Supermercado 12/08",
        TransactionChannel channel = TransactionChannel.DebitCard,
        string merchantName = "Supermercado")
        => new(userId, institutionId, accountNumber, bankTransactionId,
               amount, currency, type, rawDescription, DateTime.UtcNow, channel, merchantName);

    // ─── Positive Cases ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNewTransaction_ShouldPersistCategorizeAndReturnId()
    {
        // Arrange
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        var resultId = await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        resultId.Should().NotBeEmpty();
        await _txRepo.Received(1).AddAsync(
            Arg.Is<CanonicalTransaction>(t =>
                t.UserId == "user-1" &&
                t.Amount.Amount == 150.75m &&
                t.CategoryId == _categoryId),
            Arg.Any<CancellationToken>());
        await _balanceRepo.Received(1).AddOrUpdateAsync(Arg.Any<AccountBalance>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewTransaction_ShouldPublishTransactionNormalizedEvent()
    {
        // Arrange
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert — event published exactly once with correct fields
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TransactionNormalized>(e =>
                e.Source == "itau" &&
                e.AccountId == "acc-100" &&
                e.Amount == 150.75m &&
                e.Currency == "BRL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewTransaction_ShouldPublishEventAfterPersistence()
    {
        // Arrange — track call order
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var callOrder = new System.Collections.Generic.List<string>();
        _txRepo.AddAsync(Arg.Any<CanonicalTransaction>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("AddAsync"); return Task.CompletedTask; });
        _balanceRepo.AddOrUpdateAsync(Arg.Any<AccountBalance>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("AddOrUpdateAsync"); return Task.CompletedTask; });
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("CommitAsync"); return Task.FromResult(1); });
        _eventPublisher.PublishAsync(Arg.Any<TransactionNormalized>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("PublishAsync"); return Task.CompletedTask; });

        // Act
        await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert — persistence and commit MUST happen before publish
        callOrder.Should().ContainInOrder("AddAsync", "AddOrUpdateAsync", "CommitAsync", "PublishAsync");
    }

    [Fact]
    public async Task Handle_WhenCommitAsyncThrows_ShouldNotPublishEventAndPropagateException()
    {
        // Arrange
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB commit failure"));

        // Act
        var act = async () => await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DB commit failure*");
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<TransactionNormalized>(), Arg.Any<CancellationToken>());
    }

    // ─── Negative / Deduplication Cases ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTransactionAlreadyExistsByHash_ShouldReturnExistingIdAndNotPublish()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns(existingId);

        // Act
        var resultId = await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        resultId.Should().Be(existingId);
        await _txRepo.DidNotReceive().AddAsync(Arg.Any<CanonicalTransaction>(), Arg.Any<CancellationToken>());
        await _balanceRepo.DidNotReceive().AddOrUpdateAsync(Arg.Any<AccountBalance>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<TransactionNormalized>(), Arg.Any<CancellationToken>());
    }

    // ─── Edge / Error Cases ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPublisherThrows_ShouldPropagateExceptionWithoutSwallowing()
    {
        // Arrange
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _eventPublisher
            .PublishAsync(Arg.Any<TransactionNormalized>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("RabbitMQ unavailable"));

        // Act
        var act = async () => await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert — exception must propagate, not be swallowed
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RabbitMQ unavailable*");
    }

    [Fact]
    public async Task Handle_WhenNewTransactionWithCreditType_ShouldPublishWithCorrectTransactionType()
    {
        // Arrange
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        await _handler.Handle(BuildCommand(type: TransactionType.Credit, amount: 500m), CancellationToken.None);

        // Assert
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<TransactionNormalized>(e =>
                e.TransactionType == TransactionType.Credit.ToString() &&
                e.Amount == 500m),
            Arg.Any<CancellationToken>());
    }
}
