using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class AccountBalance
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public AccountIdentifier AccountInfo { get; private set; }
    public Money CurrentBalance { get; private set; }
    public DateTime LastUpdatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; } // Optimistic Concurrency Token (xmin em PostgreSQL)

    /// <summary>
    /// Dados de crédito da conta (limite, disponível, vencimento e fechamento da fatura).
    /// Preenchido apenas para contas de cartão, a partir do snapshot oficial da instituição.
    /// </summary>
    public CreditAccountInfo CreditInfo { get; private set; }

    private AccountBalance()
    {
        UserId = string.Empty;
        AccountInfo = new AccountIdentifier(string.Empty, string.Empty);
        CurrentBalance = new Money(0m, "BRL");
        CreditInfo = CreditAccountInfo.None;
    }

    private AccountBalance(Guid id, string userId, AccountIdentifier accountInfo, Money currentBalance, DateTime lastUpdatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new TransactionAggregatorDomainException("UserId e obrigatorio.");
        }

        Id = id;
        UserId = userId;
        AccountInfo = accountInfo ?? throw new TransactionAggregatorDomainException("AccountInfo e obrigatorio.");
        CurrentBalance = currentBalance ?? throw new InvalidMoneyAmountDomainException();
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        CreditInfo = CreditAccountInfo.None;
    }

    public static AccountBalance Create(string userId, AccountIdentifier accountInfo, Money initialBalance)
    {
        return new AccountBalance(
            Guid.NewGuid(),
            userId,
            accountInfo,
            initialBalance,
            DateTime.UtcNow);
    }

    public void SynchronizeWithBankSnapshot(Money officialBankBalance, DateTime snapshotTimestampUtc)
    {
        CurrentBalance = officialBankBalance ?? throw new InvalidMoneyAmountDomainException();
        LastUpdatedAtUtc = snapshotTimestampUtc;
    }

    /// <summary>
    /// Atualiza os dados de crédito da conta a partir do snapshot oficial da instituição.
    /// </summary>
    public void SynchronizeCreditData(CreditAccountInfo creditInfo)
    {
        CreditInfo = creditInfo ?? throw new TransactionAggregatorDomainException("CreditInfo e obrigatorio.");
    }

    public void ApplyTransaction(Money amount, TransactionType type)
    {
        if (amount == null)
        {
            throw new InvalidMoneyAmountDomainException();
        }

        CurrentBalance = type switch
        {
            TransactionType.Credit => CurrentBalance.Add(amount),
            TransactionType.Debit => CurrentBalance.Subtract(amount),
            _ => throw new TransactionAggregatorDomainException("Tipo de transacao invalido.")
        };

        LastUpdatedAtUtc = DateTime.UtcNow;
    }
}
