using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Constants;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private static readonly Expression<Func<CanonicalTransaction, TransactionDto>> ProjectToDto = t => new TransactionDto(
        t.Id,
        t.UserId,
        t.AccountInfo.InstitutionId,
        t.AccountInfo.AccountId,
        t.Amount.Amount,
        t.Amount.Currency,
        t.Type.ToString(),
        t.Description.CleanText,
        t.CategoryId,
        t.CategorizationSource.ToString(),
        t.IsManuallyCategorized,
        t.TransactionDateUtc,
        t.BankDetails.Channel.ToString(),
        t.BankDetails.MerchantName,
        t.Nature.ToString(),
        t.IsBillPayment,
        t.IsIgnoredInTotals,
        t.PairedTransactionId,
        t.Notes);

    private readonly TransactionAggregatorDbContext _context;

    public TransactionRepository(TransactionAggregatorDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByHashAsync(TransactionHash hash, CancellationToken cancellationToken)
    {
        return await _context.Transactions.AnyAsync(t => t.Hash == hash, cancellationToken);
    }

    public async Task<Guid?> GetIdByHashAsync(TransactionHash hash, CancellationToken cancellationToken)
    {
        var tx = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Hash == hash, cancellationToken);

        return tx?.Id;
    }

    public async Task<CanonicalTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(CanonicalTransaction transaction, CancellationToken cancellationToken)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task UpdateAsync(CanonicalTransaction transaction, CancellationToken cancellationToken)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<TransactionDto>> GetProjectedByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CanonicalTransaction>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedTransactionsResponseDto> QueryPagedByFilterAsync(TransactionFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == filter.UserId);

        query = ApplyDateFilters(query, filter.StartDate, filter.EndDate);
        query = ApplyInstitutionFilter(query, filter.InstitutionId);
        query = await ApplyCategoryFilterAsync(query, filter.CategoryId, cancellationToken);
        query = ApplyTypeFilter(query, filter.Type);
        query = ApplySearchFilter(query, filter.Search);
        query = ApplyChannelGroupFilter(query, filter.ChannelGroup);

        var totalItems = await query.CountAsync(cancellationToken);

        // Calcular sumário considerando transações operacionais ou todas caso IncludeIgnoredInTotals seja true
        var rawTotalsQuery = query.AsQueryable();
        if (!filter.IncludeIgnoredInTotals)
        {
            rawTotalsQuery = rawTotalsQuery.Where(t => !t.IsIgnoredInTotals);
        }

        var rawTotals = await rawTotalsQuery
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(x => x.Amount.Amount) })
            .ToListAsync(cancellationToken);

        decimal totalIncome = rawTotals.FirstOrDefault(x => x.Type == TransactionType.Credit)?.Total ?? 0m;
        decimal totalExpense = rawTotals.FirstOrDefault(x => x.Type == TransactionType.Debit)?.Total ?? 0m;
        decimal netBalance = totalIncome - totalExpense;

        // Buscar posição instantânea consolidada de saldos bancários e cartões do usuário
        var accountBalances = await _context.AccountBalances
            .Where(b => b.UserId == filter.UserId)
            .ToListAsync(cancellationToken);

        decimal realConsolidatedBalance = accountBalances
            .Where(b => b.CurrentBalance.Amount > 0)
            .Sum(b => b.CurrentBalance.Amount);

        decimal openCreditCards = accountBalances
            .Where(b => b.CurrentBalance.Amount < 0)
            .Sum(b => Math.Abs(b.CurrentBalance.Amount));

        decimal projectedAvailable = realConsolidatedBalance - openCreditCards;
        DateTime? lastSync = accountBalances.Count > 0 ? accountBalances.Max(b => b.LastUpdatedAtUtc) : null;

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        // Quando IncludeIgnoredInTotals = false (padrão), filtra os items também para excluir
        // transferências internas e lançamentos neutros da listagem, não apenas do sumário.
        var itemsQuery = filter.IncludeIgnoredInTotals
            ? query
            : query.Where(t => !t.IsIgnoredInTotals);

        var filteredItemsTotal = filter.IncludeIgnoredInTotals
            ? totalItems
            : await itemsQuery.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(filteredItemsTotal / (double)pageSize);

        var items = await itemsQuery
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        var summary = new TransactionSummaryDto(
            totalIncome,
            totalExpense,
            netBalance,
            filteredItemsTotal,
            realConsolidatedBalance,
            openCreditCards,
            projectedAvailable,
            lastSync);

        return new PagedTransactionsResponseDto(items, summary, page, pageSize, filteredItemsTotal, totalPages);
    }

    private static IQueryable<CanonicalTransaction> ApplyDateFilters(IQueryable<CanonicalTransaction> query, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDateUtc >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(t => t.TransactionDateUtc <= endUtc);
        }

        return query;
    }

    private static IQueryable<CanonicalTransaction> ApplyInstitutionFilter(IQueryable<CanonicalTransaction> query, string? institutionId)
    {
        if (!string.IsNullOrWhiteSpace(institutionId))
        {
            var keywords = BankAliases.GetKeywordsFor(institutionId);
            query = query.Where(BuildInstitutionFilterExpression(keywords));
        }

        return query;
    }

    private async Task<IQueryable<CanonicalTransaction>> ApplyCategoryFilterAsync(
        IQueryable<CanonicalTransaction> query,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return query;
        }

        var selectedCategoryId = categoryId.Value;
        var categoryIds = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == selectedCategoryId || c.ParentCategoryId == selectedCategoryId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        return query.Where(t => categoryIds.Contains(t.CategoryId));
    }

    private static IQueryable<CanonicalTransaction> ApplyTypeFilter(IQueryable<CanonicalTransaction> query, string? type)
    {
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TransactionType>(type, true, out var parsedType))
        {
            return query.Where(t => t.Type == parsedType);
        }

        return query;
    }

    private static IQueryable<CanonicalTransaction> ApplySearchFilter(IQueryable<CanonicalTransaction> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var searchLower = search.Trim().ToLowerInvariant();
        return query.Where(t => t.Description.CleanText.ToLower().Contains(searchLower)
                             || t.BankDetails.MerchantName.ToLower().Contains(searchLower));
    }

    private static IQueryable<CanonicalTransaction> ApplyChannelGroupFilter(IQueryable<CanonicalTransaction> query, string? channelGroup)
    {
        if (string.IsNullOrWhiteSpace(channelGroup))
        {
            return query;
        }

        var channelGroupNormalized = channelGroup.Trim().ToLowerInvariant();
        if (channelGroupNormalized is "credit" or "cartao")
        {
            return query.Where(t => t.BankDetails.Channel == TransactionChannel.CreditCard);
        }

        if (channelGroupNormalized is "account" or "saldo" or "conta")
        {
            return query.Where(t => t.BankDetails.Channel != TransactionChannel.CreditCard);
        }

        return query;
    }

    private static Expression<Func<CanonicalTransaction, bool>> BuildInstitutionFilterExpression(IReadOnlyList<string> keywords)
    {
        var parameter = Expression.Parameter(typeof(CanonicalTransaction), "t");
        var accountInfoProperty = Expression.Property(parameter, nameof(CanonicalTransaction.AccountInfo));
        var institutionIdProperty = Expression.Property(accountInfoProperty, nameof(AccountIdentifier.InstitutionId));

        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)
            ?? throw new MethodReflectionFailedDomainException("string.ToLower");
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])
            ?? throw new MethodReflectionFailedDomainException("string.Contains");

        var lowerInstitutionId = Expression.Call(institutionIdProperty, toLowerMethod);

        Expression? combined = null;

        foreach (var keyword in keywords)
        {
            var keywordConstant = Expression.Constant(keyword.ToLowerInvariant());
            var containsExpression = Expression.Call(lowerInstitutionId, containsMethod, keywordConstant);

            combined = combined == null
                ? containsExpression
                : Expression.OrElse(combined, containsExpression);
        }

        return Expression.Lambda<Func<CanonicalTransaction, bool>>(combined ?? Expression.Constant(true), parameter);
    }

    public async Task UpdateCategoryForPatternAsync(string userId, string pattern, Guid newCategoryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        var patternLower = pattern.Trim().ToLowerInvariant();
        var transactionsToUpdate = await _context.Transactions
            .Where(t => t.UserId == userId && t.Description.CleanText.ToLower().Contains(patternLower))
            .ToListAsync(cancellationToken);

        foreach (var tx in transactionsToUpdate)
        {
            tx.CategorizeManually(newCategoryId);
        }
    }

    public async Task<IEnumerable<CanonicalTransaction>> GetUnpairedTransfersCandidateAsync(string userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId &&
                        t.PairedTransactionId == null &&
                        t.TransactionDateUtc >= fromUtc &&
                        t.TransactionDateUtc <= toUtc)
            .OrderBy(t => t.TransactionDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<CanonicalTransaction> transactions, CancellationToken cancellationToken)
    {
        _context.Transactions.UpdateRange(transactions);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
