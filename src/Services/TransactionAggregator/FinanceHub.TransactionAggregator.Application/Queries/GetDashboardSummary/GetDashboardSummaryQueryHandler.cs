using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler(
    ITransactionRepository transactionRepository,
    IDashboardReadRepository dashboardReadRepository) : IGetDashboardSummaryQueryHandler
{
    /// <summary>Quantas transações recentes o feed do Dashboard exibe.</summary>
    private const int RecentTransactionsCount = 5;

    /// <summary>Quantas categorias aparecem antes de a cauda virar "Outras".</summary>
    private const int TopCategoriesCount = 8;

    /// <summary>Tamanho da série de evolução por mês.</summary>
    private const int CashFlowMonthsBack = 6;

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var dashboardFilter = new DashboardFilterDto(
            query.UserId,
            query.StartDate,
            query.EndDate,
            query.InstitutionId,
            query.IncludeIgnoredInTotals);

        // Reaproveita o cálculo que já alimenta a tela de Transações. Pedindo apenas uma página
        // de RecentTransactionsCount itens, a mesma consulta devolve os KPIs do período e o feed
        // de recentes — e Dashboard e Transações nunca divergem para o mesmo filtro.
        var paged = await transactionRepository.QueryPagedByFilterAsync(
            new TransactionFilterDto(
                UserId: query.UserId,
                Page: 1,
                PageSize: RecentTransactionsCount,
                StartDate: query.StartDate,
                EndDate: query.EndDate,
                InstitutionId: query.InstitutionId,
                IncludeIgnoredInTotals: query.IncludeIgnoredInTotals),
            cancellationToken);

        // Sequencial, não Task.WhenAll: todas estas chamadas compartilham o mesmo DbContext, e o
        // EF Core não permite operações concorrentes num único contexto.
        var categoryExpenses = await dashboardReadRepository.GetCategoryExpenseBreakdownAsync(
            dashboardFilter, TopCategoriesCount, cancellationToken);

        var monthlyCashFlow = await dashboardReadRepository.GetMonthlyCashFlowAsync(
            query.UserId, CashFlowMonthsBack, cancellationToken);

        var institutionBalances = await dashboardReadRepository.GetInstitutionBalancesAsync(
            query.UserId, cancellationToken);

        return new DashboardSummaryDto(
            UserId: query.UserId,
            Summary: paged.Summary,
            InstitutionBalances: institutionBalances,
            CategoryExpenses: categoryExpenses,
            MonthlyCashFlow: monthlyCashFlow,
            RecentTransactions: [.. paged.Items],
            GeneratedAtUtc: DateTime.UtcNow);
    }
}
