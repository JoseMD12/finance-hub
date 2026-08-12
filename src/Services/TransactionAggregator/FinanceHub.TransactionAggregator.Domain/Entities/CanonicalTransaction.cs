using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class CanonicalTransaction
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public AccountIdentifier AccountInfo { get; private set; }
    public TransactionHash Hash { get; private set; }
    public Money Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public SanitizedDescription Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public CategorizationSource CategorizationSource { get; private set; }
    public bool IsManuallyCategorized { get; private set; }
    public DateTime TransactionDateUtc { get; private set; }
    public BankTransactionDetails BankDetails { get; private set; }
    public TransactionAuditInfo AuditInfo { get; private set; }

    private CanonicalTransaction()
    {
        UserId = string.Empty;
        AccountInfo = new AccountIdentifier(string.Empty, string.Empty);
        Hash = new TransactionHash("0000000000000000000000000000000000000000000000000000000000000000");
        Amount = new Money(0m, "BRL");
        Description = SanitizedDescription.Create("NON_EMPTY");
        BankDetails = new BankTransactionDetails(string.Empty, TransactionChannel.Other, string.Empty);
        AuditInfo = new TransactionAuditInfo(DateTime.UtcNow, DateTime.UtcNow);
    }

    private CanonicalTransaction(
        Guid id,
        string userId,
        AccountIdentifier accountInfo,
        TransactionHash hash,
        Money amount,
        TransactionType type,
        SanitizedDescription description,
        Guid categoryId,
        CategorizationSource categorizationSource,
        bool isManuallyCategorized,
        DateTime transactionDateUtc,
        BankTransactionDetails bankDetails,
        TransactionAuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new TransactionAggregatorDomainException("UserId e obrigatorio.");
        }

        Id = id;
        UserId = userId;
        AccountInfo = accountInfo ?? throw new TransactionAggregatorDomainException("AccountInfo e obrigatorio.");
        Hash = hash ?? throw new InvalidTransactionHashDomainException();
        Amount = amount ?? throw new InvalidMoneyAmountDomainException();
        Type = type;
        Description = description ?? throw new TransactionAggregatorDomainException("Description e obrigatoria.");
        CategoryId = categoryId;
        CategorizationSource = categorizationSource;
        IsManuallyCategorized = isManuallyCategorized;
        TransactionDateUtc = transactionDateUtc;
        BankDetails = bankDetails ?? new BankTransactionDetails(string.Empty, TransactionChannel.Other, string.Empty);
        AuditInfo = auditInfo ?? new TransactionAuditInfo(DateTime.UtcNow, DateTime.UtcNow);
    }

    public static CanonicalTransaction Create(
        string userId,
        AccountIdentifier accountInfo,
        TransactionHash hash,
        Money amount,
        TransactionType type,
        SanitizedDescription description,
        Guid categoryId,
        CategorizationSource categorizationSource,
        DateTime transactionDateUtc,
        BankTransactionDetails bankDetails)
    {
        var now = DateTime.UtcNow;
        return new CanonicalTransaction(
            Guid.NewGuid(),
            userId,
            accountInfo,
            hash,
            amount,
            type,
            description,
            categoryId,
            categorizationSource,
            isManuallyCategorized: false,
            transactionDateUtc,
            bankDetails,
            new TransactionAuditInfo(now, now));
    }

    public void CategorizeManually(Guid newCategoryId)
    {
        if (newCategoryId == Guid.Empty)
        {
            throw new InvalidCategoryIdDomainException();
        }

        CategoryId = newCategoryId;
        CategorizationSource = CategorizationSource.UserManual;
        IsManuallyCategorized = true;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }
}
