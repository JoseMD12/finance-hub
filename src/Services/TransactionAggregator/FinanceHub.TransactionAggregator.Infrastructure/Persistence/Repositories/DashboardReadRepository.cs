using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Constants;
using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;

public sealed class DashboardReadRepository(TransactionAggregatorDbContext context) : IDashboardReadRepository
{
    private const string OtherCategoriesName = "Outras";
    private const string OtherCategoriesColorToken = "slate";
    private const string OtherCategoriesIconKey = "ellipsis";

    public async Task<IReadOnlyList<CategoryExpenseDto>> GetCategoryExpenseBreakdownAsync(
        DashboardFilterDto filter,
        int topCount,
        CancellationToken cancellationToken)
    {
        var query = BuildBaseQuery(filter)
            .Where(t => t.Type == TransactionType.Debit);

        // GroupBy traduzido para SQL: o banco devolve uma linha por categoria, não as transações.
        var grouped = await query
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => t.Amount.Amount) })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return [];
        }

        var totalExpense = grouped.Sum(g => g.Amount);
        if (totalExpense <= 0m)
        {
            return [];
        }

        var categoryIds = grouped.Select(g => g.CategoryId).ToList();
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.ColorToken, c.IconKey })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var ranked = grouped
            .OrderByDescending(g => g.Amount)
            .ToList();

        var head = ranked.Take(topCount).ToList();
        var tail = ranked.Skip(topCount).ToList();

        var result = head
            .Select(g =>
            {
                var found = categories.TryGetValue(g.CategoryId, out var category);
                return new CategoryExpenseDto(
                    CategoryId: g.CategoryId,
                    CategoryName: found ? category!.Name : OtherCategoriesName,
                    ColorToken: found ? category!.ColorToken : OtherCategoriesColorToken,
                    IconKey: found ? category!.IconKey : OtherCategoriesIconKey,
                    AmountBrl: g.Amount,
                    Percentage: CalculatePercentage(g.Amount, totalExpense));
            })
            .ToList();

        if (tail.Count > 0)
        {
            var tailAmount = tail.Sum(g => g.Amount);
            result.Add(new CategoryExpenseDto(
                CategoryId: Guid.Empty,
                CategoryName: OtherCategoriesName,
                ColorToken: OtherCategoriesColorToken,
                IconKey: OtherCategoriesIconKey,
                AmountBrl: tailAmount,
                Percentage: CalculatePercentage(tailAmount, totalExpense)));
        }

        return result;
    }

    public async Task<IReadOnlyList<MonthlyCashFlowPointDto>> GetMonthlyCashFlowAsync(
        string userId,
        int monthsBack,
        CancellationToken cancellationToken)
    {
        // Início do mês, monthsBack-1 meses atrás: uma janela de monthsBack meses incluindo o atual.
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(monthsBack - 1));

        var grouped = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                     && !t.IsIgnoredInTotals
                     && t.TransactionDateUtc >= firstMonth)
            .GroupBy(t => new { t.TransactionDateUtc.Year, t.TransactionDateUtc.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Income = g.Sum(t => t.Type == TransactionType.Credit ? t.Amount.Amount : 0m),
                Expense = g.Sum(t => t.Type == TransactionType.Debit ? t.Amount.Amount : 0m)
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. grouped
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .Select(g => new MonthlyCashFlowPointDto(
                    g.Year,
                    g.Month,
                    g.Income,
                    g.Expense,
                    g.Income - g.Expense))
        ];
    }

    public async Task<IReadOnlyList<InstitutionBalanceDto>> GetInstitutionBalancesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var balances = await context.AccountBalances
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        return
        [
            .. balances
                .OrderByDescending(b => b.CurrentBalance.Amount)
                .Select(b => new InstitutionBalanceDto(
                    InstitutionId: b.AccountInfo.InstitutionId,
                    AccountNumber: b.AccountInfo.AccountId,
                    BalanceBrl: b.CurrentBalance.Amount,
                    Currency: b.CurrentBalance.Currency,
                    IsCreditCard: b.CreditInfo.IsCreditCard,
                    CreditLimit: b.CreditInfo.CreditLimit,
                    AvailableCreditLimit: b.CreditInfo.AvailableCreditLimit,
                    UsedCreditLimit: b.CreditInfo.UsedCreditLimit,
                    InvoiceDueDateUtc: b.CreditInfo.InvoiceDueDateUtc,
                    LastUpdatedAtUtc: b.LastUpdatedAtUtc))
        ];
    }

    private IQueryable<CanonicalTransaction> BuildBaseQuery(DashboardFilterDto filter)
    {
        var query = context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == filter.UserId);

        if (!filter.IncludeIgnoredInTotals)
        {
            query = query.Where(t => !t.IsIgnoredInTotals);
        }

        if (filter.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDateUtc >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDateUtc <= endUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.InstitutionId))
        {
            var keywords = BankAliases.GetKeywordsFor(filter.InstitutionId);
            query = query.Where(t => keywords.Any(k => t.AccountInfo.InstitutionId.ToLower().Contains(k)));
        }

        return query;
    }

    private static decimal CalculatePercentage(decimal amount, decimal total)
        => Math.Round(amount / total * 100m, 2, MidpointRounding.AwayFromZero);
}
