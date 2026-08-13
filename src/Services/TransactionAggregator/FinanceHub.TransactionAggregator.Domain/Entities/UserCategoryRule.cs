using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class UserCategoryRule
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string Pattern { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private UserCategoryRule()
    {
        UserId = string.Empty;
        Pattern = string.Empty;
    }

    public UserCategoryRule(Guid id, string userId, string pattern, Guid categoryId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new TransactionAggregatorDomainException("UserId e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new TransactionAggregatorDomainException("Pattern e obrigatorio.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new InvalidCategoryIdDomainException();
        }

        Id = id;
        UserId = userId;
        Pattern = pattern.Trim().ToUpperInvariant();
        CategoryId = categoryId;
        CreatedAtUtc = createdAtUtc;
    }

    public static UserCategoryRule Create(string userId, string pattern, Guid categoryId)
    {
        return new UserCategoryRule(Guid.NewGuid(), userId, pattern, categoryId, DateTime.UtcNow);
    }
}
