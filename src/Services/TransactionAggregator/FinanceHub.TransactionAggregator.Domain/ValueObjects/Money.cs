using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidCurrencyDomainException();
        }

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money MoneyToAdd)
    {
        EnsureSameCurrency(MoneyToAdd);
        return new Money(Amount + MoneyToAdd.Amount, Currency);
    }

    public Money Subtract(Money MoneyToSubtract)
    {
        EnsureSameCurrency(MoneyToSubtract);
        return new Money(Amount - MoneyToSubtract.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new CurrencyMismatchDomainException();
        }
    }
}
