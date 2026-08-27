using System;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

/// <summary>
/// Dados de crédito de uma conta de cartão, capturados do snapshot oficial da instituição.
/// Limite e limite disponível são fatos <b>da conta</b>, não de cada transação — por isso
/// vivem aqui, em <see cref="AccountBalance"/>, e não em <see cref="BankTransactionDetails"/>.
/// </summary>
public class CreditAccountInfo
{
    public bool IsCreditCard { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public decimal? AvailableCreditLimit { get; private set; }

    /// <summary>Vencimento da fatura atual, quando a instituição informa.</summary>
    public DateTime? InvoiceDueDateUtc { get; private set; }

    /// <summary>
    /// Fechamento da fatura atual. Nem todo connector devolve este campo; quando ausente,
    /// é derivado do vencimento ou configurado pelo usuário por cartão.
    /// </summary>
    public DateTime? InvoiceClosingDateUtc { get; private set; }

    /// <summary>
    /// Limite já consumido. Devolve <c>null</c> quando algum dos limites é desconhecido, e
    /// nunca devolve negativo — connectors chegam a informar disponível maior que o total
    /// logo após um pagamento de fatura ainda não compensado.
    /// </summary>
    public decimal? UsedCreditLimit =>
        CreditLimit.HasValue && AvailableCreditLimit.HasValue
            ? Math.Max(0m, CreditLimit.Value - AvailableCreditLimit.Value)
            : null;

    /// <summary>Instância neutra para contas que não são de crédito.</summary>
    public static CreditAccountInfo None => new(isCreditCard: false);

    private CreditAccountInfo()
    {
    }

    public CreditAccountInfo(
        bool isCreditCard,
        decimal? creditLimit = null,
        decimal? availableCreditLimit = null,
        DateTime? invoiceDueDateUtc = null,
        DateTime? invoiceClosingDateUtc = null)
    {
        IsCreditCard = isCreditCard;
        CreditLimit = creditLimit;
        AvailableCreditLimit = availableCreditLimit;
        InvoiceDueDateUtc = NormalizeToUtc(invoiceDueDateUtc);
        InvoiceClosingDateUtc = NormalizeToUtc(invoiceClosingDateUtc);
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
