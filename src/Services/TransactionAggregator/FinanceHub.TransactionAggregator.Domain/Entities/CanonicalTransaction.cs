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
    public TransactionNature Nature { get; private set; }
    public bool IsBillPayment { get; private set; }
    public bool IsIgnoredInTotals { get; private set; }
    public Guid? PairedTransactionId { get; private set; }
    public string? Notes { get; private set; }

    private CanonicalTransaction()
    {
        UserId = string.Empty;
        AccountInfo = new AccountIdentifier(string.Empty, string.Empty);
        Hash = new TransactionHash("0000000000000000000000000000000000000000000000000000000000000000");
        Amount = new Money(0m, "BRL");
        Description = SanitizedDescription.Create("NON_EMPTY");
        BankDetails = new BankTransactionDetails(string.Empty, TransactionChannel.Other, string.Empty);
        AuditInfo = new TransactionAuditInfo(DateTime.UtcNow, DateTime.UtcNow);
        Nature = TransactionNature.Operating;
        IsBillPayment = false;
        IsIgnoredInTotals = false;
        PairedTransactionId = null;
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
        Nature = TransactionNature.Operating;
        IsBillPayment = false;
        IsIgnoredInTotals = false;
        PairedTransactionId = null;
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

    public void MarkAsInternalTransfer(Guid pairedTransactionId)
    {
        if (pairedTransactionId == Guid.Empty)
        {
            throw new TransactionAggregatorDomainException("PairedTransactionId invalido.");
        }

        Nature = TransactionNature.Transfer;
        IsIgnoredInTotals = true;
        PairedTransactionId = pairedTransactionId;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    /// <summary>
    /// Aplica a natureza econômica herdada da categoria resolvida e deriva dela a neutralidade
    /// nos totais.
    ///
    /// Substitui a antiga classificação por casamento de texto no handler de ingestão, que
    /// dependia de valores específicos de um usuário. Aqui não há conhecimento de quem é o
    /// titular: dinheiro que apenas muda de lugar — transferência, investimento ou ajuste — não
    /// é gasto de vida e não entra nos totais.
    /// </summary>
    public void ApplyNature(TransactionNature nature)
    {
        Nature = nature;

        if (IsNeutralByNature(nature))
        {
            IsIgnoredInTotals = true;
        }

        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    /// <summary>
    /// Naturezas que representam dinheiro em trânsito, conforme
    /// `.agents/specs/transit-transfers-and-neutrality-engine-spec.md` §2.1.
    /// </summary>
    public static bool IsNeutralByNature(TransactionNature nature) =>
        nature is TransactionNature.Transfer
                or TransactionNature.BillPayment
                or TransactionNature.Investment
                or TransactionNature.Adjustment;

    public void MarkAsBillPayment()
    {
        Nature = TransactionNature.BillPayment;
        IsBillPayment = true;
        IsIgnoredInTotals = true;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void UnmarkBillPayment()
    {
        IsBillPayment = false;
        IsIgnoredInTotals = false;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void MarkAsTransitMoney(Guid? pairedTransactionId = null)
    {
        IsIgnoredInTotals = true;
        PairedTransactionId = pairedTransactionId;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void ToggleIgnoreInTotals(bool ignore)
    {
        IsIgnoredInTotals = ignore;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void UpdateNotes(string? notes)
    {
        if (!string.IsNullOrWhiteSpace(notes) && notes.Length > 500)
        {
            throw new TransactionAggregatorDomainException("Observacoes/notas nao podem exceder 500 caracteres.");
        }

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }
}
