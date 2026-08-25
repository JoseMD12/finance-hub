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

public class AccountBalanceRepository : IAccountBalanceRepository
{
    private readonly TransactionAggregatorDbContext _context;

    public AccountBalanceRepository(TransactionAggregatorDbContext context)
    {
        _context = context;
    }

    public async Task<AccountBalance?> GetByUserAndAccountAsync(string userId, AccountIdentifier accountInfo, CancellationToken cancellationToken)
    {
        return await _context.AccountBalances
            .FirstOrDefaultAsync(b => b.UserId == userId &&
                                     b.AccountInfo.InstitutionId == accountInfo.InstitutionId &&
                                     b.AccountInfo.AccountId == accountInfo.AccountId, cancellationToken);
    }

    public async Task<IEnumerable<AccountBalanceDto>> GetProjectedByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.AccountBalances
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => new AccountBalanceDto(
                b.AccountInfo.InstitutionId,
                b.AccountInfo.AccountId,
                b.CurrentBalance.Amount,
                b.CurrentBalance.Currency,
                b.LastUpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountBalance>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.AccountBalances
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateAsync(AccountBalance balance, CancellationToken cancellationToken)
    {
        var entry = _context.Entry(balance);
        if (entry.State == EntityState.Detached)
        {
            var existing = await _context.AccountBalances.FirstOrDefaultAsync(b => b.Id == balance.Id, cancellationToken);
            if (existing == null)
            {
                await _context.AccountBalances.AddAsync(balance, cancellationToken);
            }
        }
    }
}
