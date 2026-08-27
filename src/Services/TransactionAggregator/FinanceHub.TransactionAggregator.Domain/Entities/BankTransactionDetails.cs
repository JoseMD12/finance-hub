using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class BankTransactionDetails
{
    public string BankTransactionId { get; private set; }
    public TransactionChannel Channel { get; private set; }
    public string MerchantName { get; private set; }

    /// <summary>
    /// Vencimento da fatura à qual esta compra pertence, quando a instituição informa.
    /// Só é preenchido em lançamentos de cartão de crédito.
    /// </summary>
    public DateTime? InvoiceDueDateUtc { get; private set; }

    /// <summary>Posição desta parcela (ex.: 2 em "2/6"), quando o connector informa.</summary>
    public int? CurrentInstallment { get; private set; }

    /// <summary>Total de parcelas da compra (ex.: 6 em "2/6"), quando o connector informa.</summary>
    public int? TotalInstallments { get; private set; }

    /// <summary>Compra parcelada é aquela com mais de uma parcela no total.</summary>
    public bool IsInstallment => TotalInstallments > 1;

    private BankTransactionDetails()
    {
        BankTransactionId = string.Empty;
        MerchantName = string.Empty;
    }

    public BankTransactionDetails(
        string bankTransactionId,
        TransactionChannel channel,
        string merchantName,
        DateTime? invoiceDueDateUtc = null,
        int? currentInstallment = null,
        int? totalInstallments = null)
    {
        ValidateInstallments(currentInstallment, totalInstallments);

        BankTransactionId = bankTransactionId ?? string.Empty;
        Channel = channel;
        MerchantName = merchantName ?? string.Empty;
        InvoiceDueDateUtc = NormalizeToUtc(invoiceDueDateUtc);
        CurrentInstallment = currentInstallment;
        TotalInstallments = totalInstallments;
    }

    private static void ValidateInstallments(int? currentInstallment, int? totalInstallments)
    {
        if (currentInstallment is <= 0 || totalInstallments is <= 0)
        {
            throw new InvalidInstallmentDomainException();
        }

        if (currentInstallment.HasValue && totalInstallments.HasValue &&
            currentInstallment.Value > totalInstallments.Value)
        {
            throw new InvalidInstallmentDomainException();
        }
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        // Local converte de fato; Unspecified vem das APIs bancárias já em UTC e só é rotulado.
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
