using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
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

    public Task UpdateAsync(CanonicalTransaction transaction, CancellationToken cancellationToken)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<TransactionDto>> GetProjectedByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
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
                t.BankDetails.MerchantName))
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

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDateUtc >= DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc));
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDateUtc <= DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc));
        }

        if (!string.IsNullOrWhiteSpace(filter.InstitutionId))
        {
            query = query.Where(t => t.AccountInfo.InstitutionId == filter.InstitutionId);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Type) && Enum.TryParse<TransactionType>(filter.Type, true, out var parsedType))
        {
            query = query.Where(t => t.Type == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Description.CleanText.ToLower().Contains(searchLower)
                                  || t.BankDetails.MerchantName.ToLower().Contains(searchLower));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        // Calcular sumário do período selecionado
        var rawTotals = await query
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(x => x.Amount.Amount) })
            .ToListAsync(cancellationToken);

        decimal totalIncome = rawTotals.FirstOrDefault(x => x.Type == TransactionType.Credit)?.Total ?? 0m;
        decimal totalExpense = rawTotals.FirstOrDefault(x => x.Type == TransactionType.Debit)?.Total ?? 0m;
        decimal netBalance = totalIncome - totalExpense;

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .OrderByDescending(t => t.TransactionDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
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
                t.BankDetails.MerchantName))
            .ToListAsync(cancellationToken);

        var summary = new TransactionSummaryDto(totalIncome, totalExpense, netBalance, totalItems);

        return new PagedTransactionsResponseDto(items, summary, page, pageSize, totalItems, totalPages);
    }
}
