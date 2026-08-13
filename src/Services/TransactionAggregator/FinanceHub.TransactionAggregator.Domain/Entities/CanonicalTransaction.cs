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
        CanonicalTransactionCreationParams creationParams,
        TransactionAuditInfo auditInfo)
    {
        if (string.IsNullOrWhiteSpace(creationParams.UserId))
        {
            throw new TransactionAggregatorDomainException("UserId e obrigatorio.");
        }

        Id = id;
        UserId = creationParams.UserId;
        AccountInfo = creationParams.AccountInfo ?? throw new TransactionAggregatorDomainException("AccountInfo e obrigatorio.");
        Hash = creationParams.Hash ?? throw new InvalidTransactionHashDomainException();
        Amount = creationParams.Amount ?? throw new InvalidMoneyAmountDomainException();
        Type = creationParams.Type;
        Description = creationParams.Description ?? throw new TransactionAggregatorDomainException("Description e obrigatoria.");
        CategoryId = creationParams.CategoryId;
        CategorizationSource = creationParams.CategorizationSource;
        IsManuallyCategorized = false;
        TransactionDateUtc = creationParams.TransactionDateUtc;
        BankDetails = creationParams.BankDetails ?? new BankTransactionDetails(string.Empty, TransactionChannel.Other, string.Empty);
        AuditInfo = auditInfo ?? new TransactionAuditInfo(DateTime.UtcNow, DateTime.UtcNow);
    }

    public static CanonicalTransaction Create(CanonicalTransactionCreationParams creationParams)
    {
        var now = DateTime.UtcNow;
        return new CanonicalTransaction(
            Guid.NewGuid(),
            creationParams,
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
