using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Queries.GetDashboardSummary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Application;

/// <summary>
/// O Dashboard reaproveita <c>QueryPagedByFilterAsync</c>, que já calcula os KPIs com as regras
/// de neutralidade aplicadas. Estes testes travam esse reaproveitamento — se o handler passar a
/// recalcular por conta própria, Dashboard e Transações divergem para o mesmo período, que é
/// exatamente o defeito que a slice existe para evitar.
/// </summary>
public class GetDashboardSummaryQueryHandlerTests
{
    private const string UserId = "user-1";
    private const int RecentTransactionsCount = 5;

    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IDashboardReadRepository _dashboardReadRepository = Substitute.For<IDashboardReadRepository>();
    private readonly GetDashboardSummaryQueryHandler _handler;

    public GetDashboardSummaryQueryHandlerTests()
    {
        _transactionRepository
            .QueryPagedByFilterAsync(Arg.Any<TransactionFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(BuildPagedResponse());

        _dashboardReadRepository
            .GetCategoryExpenseBreakdownAsync(Arg.Any<DashboardFilterDto>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _dashboardReadRepository
            .GetMonthlyCashFlowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _dashboardReadRepository
            .GetInstitutionBalancesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new GetDashboardSummaryQueryHandler(_transactionRepository, _dashboardReadRepository);
    }

    // ─── Reaproveitamento do cálculo existente ────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReuseTransactionRepositorySummaryInsteadOfRecomputing()
    {
        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        result.Summary.TotalIncome.Should().Be(8500m);
        result.Summary.TotalExpense.Should().Be(5400m);
        result.Summary.RealConsolidatedBalanceBrl.Should().Be(1650.59m);
    }

    [Fact]
    public async Task Handle_ShouldRequestOnlyTheRecentTransactionsItNeeds()
    {
        await _handler.Handle(BuildQuery(), CancellationToken.None);

        await _transactionRepository.Received(1).QueryPagedByFilterAsync(
            Arg.Is<TransactionFilterDto>(f => f.PageSize == RecentTransactionsCount && f.Page == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldForwardPeriodAndInstitutionFiltersDownstream()
    {
        var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);

        await _handler.Handle(BuildQuery(start, end, "itau"), CancellationToken.None);

        await _transactionRepository.Received(1).QueryPagedByFilterAsync(
            Arg.Is<TransactionFilterDto>(f =>
                f.UserId == UserId &&
                f.StartDate == start &&
                f.EndDate == end &&
                f.InstitutionId == "itau"),
            Arg.Any<CancellationToken>());

        await _dashboardReadRepository.Received(1).GetCategoryExpenseBreakdownAsync(
            Arg.Is<DashboardFilterDto>(f =>
                f.UserId == UserId &&
                f.StartDate == start &&
                f.EndDate == end &&
                f.InstitutionId == "itau"),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPropagateIncludeIgnoredInTotalsFlag()
    {
        await _handler.Handle(BuildQuery(includeIgnored: true), CancellationToken.None);

        await _transactionRepository.Received(1).QueryPagedByFilterAsync(
            Arg.Is<TransactionFilterDto>(f => f.IncludeIgnoredInTotals),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnRecentTransactionsFromTheSameQuery()
    {
        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        result.RecentTransactions.Should().HaveCount(2);
        result.RecentTransactions.First().Description.Should().Be("SUPERMERCADO");
    }

    // ─── Robustez ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserHasNoData_ShouldReturnZeroedSummaryWithoutThrowing()
    {
        _transactionRepository
            .QueryPagedByFilterAsync(Arg.Any<TransactionFilterDto>(), Arg.Any<CancellationToken>())
            .Returns(new PagedTransactionsResponseDto(
                Items: [],
                Summary: new TransactionSummaryDto(0m, 0m, 0m, 0),
                Page: 1,
                PageSize: RecentTransactionsCount,
                TotalItems: 0,
                TotalPages: 0));

        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        result.Summary.TotalIncome.Should().Be(0m);
        result.RecentTransactions.Should().BeEmpty();
        result.CategoryExpenses.Should().BeEmpty();
        result.MonthlyCashFlow.Should().BeEmpty();
        result.InstitutionBalances.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldStampGenerationTimestamp()
    {
        var before = DateTime.UtcNow;

        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        result.GeneratedAtUtc.Should().BeOnOrAfter(before);
        result.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_ShouldSurfaceCategoryBreakdownAndCashFlow()
    {
        _dashboardReadRepository
            .GetCategoryExpenseBreakdownAsync(Arg.Any<DashboardFilterDto>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new CategoryExpenseDto(Guid.NewGuid(), "Alimentação", "emerald", "utensils", 1200m, 60m),
                new CategoryExpenseDto(Guid.NewGuid(), "Transporte", "sky", "car", 800m, 40m)
            ]);

        _dashboardReadRepository
            .GetMonthlyCashFlowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([new MonthlyCashFlowPointDto(2026, 8, 8500m, 5400m, 3100m)]);

        var result = await _handler.Handle(BuildQuery(), CancellationToken.None);

        result.CategoryExpenses.Should().HaveCount(2);
        result.CategoryExpenses.Sum(c => c.Percentage).Should().Be(100m);
        result.MonthlyCashFlow.Should().ContainSingle();
        result.MonthlyCashFlow[0].NetBrl.Should().Be(3100m);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetDashboardSummaryQuery BuildQuery(
        DateTime? start = null,
        DateTime? end = null,
        string? institutionId = null,
        bool includeIgnored = false)
        => new(UserId, start, end, institutionId, includeIgnored);

    private static PagedTransactionsResponseDto BuildPagedResponse()
        => new(
            Items:
            [
                BuildTransaction("SUPERMERCADO"),
                BuildTransaction("POSTO IPIRANGA")
            ],
            Summary: new TransactionSummaryDto(
                TotalIncome: 8500m,
                TotalExpense: 5400m,
                NetBalance: 3100m,
                TotalCount: 42,
                RealConsolidatedBalanceBrl: 1650.59m,
                TotalOpenCreditCardsBrl: 4316.78m,
                ProjectedAvailableBalanceBrl: -2666.19m,
                LastSyncAtUtc: new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc)),
            Page: 1,
            PageSize: RecentTransactionsCount,
            TotalItems: 42,
            TotalPages: 9);

    private static TransactionDto BuildTransaction(string description)
        => new(
            Id: Guid.NewGuid(),
            UserId: UserId,
            InstitutionId: "itau",
            AccountNumber: "acc-1",
            Amount: 120m,
            Currency: "BRL",
            Type: "Debit",
            Description: description,
            CategoryId: Guid.NewGuid(),
            CategorizationSource: "GlobalRule",
            IsManuallyCategorized: false,
            TransactionDateUtc: new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            Channel: "CreditCard",
            MerchantName: description);
}
