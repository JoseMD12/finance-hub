using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceHub.Tests.Unit.Services.TransactionAggregator.Infrastructure;

public sealed class DashboardReadRepositoryTests : IDisposable
{
    private const string UserId = "user-1";
    private const int MonthsBack = 6;

    private readonly TransactionAggregatorDbContext _context;
    private readonly DashboardReadRepository _repository;
    private readonly DateTime _thisMonth;
    private readonly DateTime _lastMonth;

    public DashboardReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TransactionAggregatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TransactionAggregatorDbContext(options);
        _repository = new DashboardReadRepository(_context);

        var now = DateTime.UtcNow;
        _thisMonth = new DateTime(now.Year, now.Month, 1, 12, 0, 0, DateTimeKind.Utc);
        _lastMonth = _thisMonth.AddMonths(-1);
    }

    [Fact]
    public async Task GetMonthlyCashFlowAsync_WhenInstitutionFilterIsSet_ShouldOnlyAggregateThatInstitution()
    {
        await SeedAsync(
            Build("itau", 100m, TransactionType.Debit, _thisMonth),
            Build("inter", 300m, TransactionType.Debit, _thisMonth),
            Build("inter", 1000m, TransactionType.Credit, _lastMonth));

        var result = await _repository.GetMonthlyCashFlowAsync(
            new DashboardFilterDto(UserId, InstitutionId: "inter"), MonthsBack, CancellationToken.None);

        result.Sum(p => p.ExpenseBrl).Should().Be(300m);
        result.Sum(p => p.IncomeBrl).Should().Be(1000m);
    }

    [Fact]
    public async Task GetMonthlyCashFlowAsync_WhenPeriodFilterIsSet_ShouldStillReturnTheWholeWindow()
    {
        await SeedAsync(
            Build("inter", 300m, TransactionType.Debit, _thisMonth),
            Build("inter", 1000m, TransactionType.Credit, _lastMonth));

        var filter = new DashboardFilterDto(UserId, StartDate: _thisMonth, EndDate: _thisMonth);

        var result = await _repository.GetMonthlyCashFlowAsync(filter, MonthsBack, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMonthlyCashFlowAsync_ShouldLeaveNeutralTransactionsOutUnlessRequested()
    {
        var transfer = Build("inter", 500m, TransactionType.Debit, _thisMonth);
        transfer.ApplyNature(TransactionNature.Transfer);
        await SeedAsync(transfer, Build("inter", 300m, TransactionType.Debit, _thisMonth));

        var excluded = await _repository.GetMonthlyCashFlowAsync(
            new DashboardFilterDto(UserId), MonthsBack, CancellationToken.None);
        var included = await _repository.GetMonthlyCashFlowAsync(
            new DashboardFilterDto(UserId, IncludeIgnoredInTotals: true), MonthsBack, CancellationToken.None);

        excluded.Sum(p => p.ExpenseBrl).Should().Be(300m);
        included.Sum(p => p.ExpenseBrl).Should().Be(800m);
    }

    [Fact]
    public async Task GetInstitutionBalancesAsync_ShouldExposeTheCustomizedLimitNextToTheBankLimit()
    {
        var card = AccountBalance.Create(UserId, new AccountIdentifier("itau", "acc-card"), new Money(-2000m, "BRL"));
        card.SynchronizeCreditData(new CreditAccountInfo(
            isCreditCard: true, creditLimit: 10000m, availableCreditLimit: 8000m, customizedCreditLimit: 3000m));
        _context.AccountBalances.Add(card);
        await _context.SaveChangesAsync();

        var result = await _repository.GetInstitutionBalancesAsync(UserId, CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.CreditLimit.Should().Be(10000m);
        dto.UsedCreditLimit.Should().Be(2000m);
        dto.CustomizedCreditLimit.Should().Be(3000m);
    }

    private async Task SeedAsync(params CanonicalTransaction[] transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    private static CanonicalTransaction Build(string institutionId, decimal amount, TransactionType type, DateTime dateUtc)
        => CanonicalTransaction.Create(new CanonicalTransactionCreationParams(
            UserId,
            new AccountIdentifier(institutionId, "acc-1"),
            new TransactionHash(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")),
            new Money(amount, "BRL"),
            type,
            SanitizedDescription.Create("LANCAMENTO DE TESTE"),
            Guid.NewGuid(),
            CategorizationSource.GlobalRule,
            dateUtc,
            new BankTransactionDetails(Guid.NewGuid().ToString(), TransactionChannel.Pix, "Merchant")));

    public void Dispose() => _context.Dispose();
}
