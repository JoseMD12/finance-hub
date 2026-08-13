using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;

public class UserCategoryRuleRepository : IUserCategoryRuleRepository
{
    private readonly TransactionAggregatorDbContext _context;

    public UserCategoryRuleRepository(TransactionAggregatorDbContext context)
    {
        _context = context;
    }

    public async Task<UserCategoryRule?> FindByPatternAsync(string userId, string cleanPattern, CancellationToken cancellationToken)
    {
        var upperPattern = cleanPattern.Trim().ToUpperInvariant();
        return await _context.UserCategoryRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Pattern == upperPattern, cancellationToken);
    }

    public async Task AddOrUpdateAsync(UserCategoryRule rule, CancellationToken cancellationToken)
    {
        var existing = await _context.UserCategoryRules
            .FirstOrDefaultAsync(r => r.UserId == rule.UserId && r.Pattern == rule.Pattern, cancellationToken);

        if (existing == null)
        {
            await _context.UserCategoryRules.AddAsync(rule, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
