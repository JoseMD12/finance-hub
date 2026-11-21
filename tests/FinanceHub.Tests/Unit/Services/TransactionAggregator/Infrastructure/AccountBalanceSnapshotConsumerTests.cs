using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Unit.Services.TransactionAggregator.Infrastructure;

public sealed class AccountBalanceSnapshotConsumerTests : IDisposable
{
    private const string UserId = "user-1";

    private readonly TransactionAggregatorDbContext _context;
    private readonly AccountBalanceSnapshotSynchronizedConsumer _consumer;

    public AccountBalanceSnapshotConsumerTests()
    {
        var options = new DbContextOptionsBuilder<TransactionAggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TransactionAggregatorDbContext(options);
        _consumer = new AccountBalanceSnapshotSynchronizedConsumer(
            _context, NullLogger<AccountBalanceSnapshotSynchronizedConsumer>.Instance);
    }

    [Fact]
    public async Task Consume_WhenCardHasCustomizedLimit_ShouldPersistItAlongsideTheBankLimit()
    {
        await _consumer.Consume(BuildContext(BuildCardItem(customizedCreditLimit: 3000m)));

        var balance = await _context.AccountBalances.AsNoTracking().SingleAsync();
        balance.CreditInfo.CreditLimit.Should().Be(10000m);
        balance.CreditInfo.CustomizedCreditLimit.Should().Be(3000m);
    }

    [Fact]
    public async Task Consume_WhenCustomizationIsRemoved_ShouldClearThePreviousCustomizedLimit()
    {
        await _consumer.Consume(BuildContext(BuildCardItem(customizedCreditLimit: 3000m)));
        _context.ChangeTracker.Clear();

        await _consumer.Consume(BuildContext(BuildCardItem(customizedCreditLimit: null)));

        var balance = await _context.AccountBalances.AsNoTracking().SingleAsync();
        balance.CreditInfo.CustomizedCreditLimit.Should().BeNull();
    }

    private static AccountBalanceSnapshotItem BuildCardItem(decimal? customizedCreditLimit)
        => new(
            AccountId: "acc-card",
            InstitutionId: "itau",
            AccountType: "CREDIT",
            CurrentBalance: -2000m,
            Currency: "BRL",
            SnapshotAtUtc: DateTime.UtcNow,
            IsCreditCard: true,
            CreditLimit: 10000m,
            AvailableCreditLimit: 8000m,
            CustomizedCreditLimit: customizedCreditLimit);

    private static ConsumeContext<AccountBalanceSnapshotSynchronized> BuildContext(AccountBalanceSnapshotItem item)
    {
        var context = Substitute.For<ConsumeContext<AccountBalanceSnapshotSynchronized>>();
        context.Message.Returns(new AccountBalanceSnapshotSynchronized(UserId, [item], DateTime.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    public void Dispose() => _context.Dispose();
}
