using System;

namespace FinanceHub.TransactionAggregator.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected DomainException(string message, string errorCode = "DOMAIN_ERROR", int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public class TransactionAggregatorDomainException : DomainException
{
    public TransactionAggregatorDomainException(string message, string errorCode = "TRANSACTION_AGGREGATOR_DOMAIN_ERROR", int statusCode = 400)
        : base(message, errorCode, statusCode)
    {
    }
}

public class InvalidCurrencyDomainException : TransactionAggregatorDomainException
{
    public InvalidCurrencyDomainException()
        : base("Moeda obrigatoria nao informada ou invalida.", "INVALID_CURRENCY", 400)
    {
    }
}

public class CurrencyMismatchDomainException : TransactionAggregatorDomainException
{
    public CurrencyMismatchDomainException()
        : base("Nao e possivel realizar operacoes financeiras entre moedas distintas.", "CURRENCY_MISMATCH", 400)
    {
    }
}

public class InvalidTransactionHashDomainException : TransactionAggregatorDomainException
{
    public InvalidTransactionHashDomainException()
        : base("Hash SHA-256 da transacao deve ter exatamente 64 caracteres hexadecimais.", "INVALID_TRANSACTION_HASH", 400)
    {
    }
}

public class InvalidMoneyAmountDomainException : TransactionAggregatorDomainException
{
    public InvalidMoneyAmountDomainException()
        : base("Valor monetario invalido.", "INVALID_MONEY_AMOUNT", 400)
    {
    }
}

public class CanonicalTransactionNotFoundDomainException : TransactionAggregatorDomainException
{
    public CanonicalTransactionNotFoundDomainException()
        : base("Transacao canonica nao encontrada.", "TRANSACTION_NOT_FOUND", 404)
    {
    }
}

public class InvalidCategoryIdDomainException : TransactionAggregatorDomainException
{
    public InvalidCategoryIdDomainException()
        : base("Identificador de categoria invalido.", "INVALID_CATEGORY_ID", 400)
    {
    }
}
