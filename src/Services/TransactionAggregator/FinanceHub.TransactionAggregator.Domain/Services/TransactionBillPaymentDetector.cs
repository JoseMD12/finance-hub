using System;
using System.Linq;

namespace FinanceHub.TransactionAggregator.Domain.Services;

/// <summary>
/// Reconhece pagamento de fatura de cartão pelo vocabulário usado pelos bancos brasileiros.
///
/// É conhecimento de <b>mercado</b>, não de usuário: qualquer correntista no Brasil vê alguma
/// dessas expressões no extrato. Por isso pode viver em código, ao contrário de qualquer padrão
/// derivado dos dados de uma pessoa específica, que pertence ao banco de dados como regra do
/// usuário.
///
/// Importa porque pagamento de fatura debitado da conta não é gasto novo — a despesa já foi
/// contada quando a compra entrou na fatura. Sem isto, todo gasto no cartão seria contado duas
/// vezes.
/// </summary>
public static class TransactionBillPaymentDetector
{
    private static readonly string[] BillPaymentKeywords =
    [
        "PAGAMENTO DE FATURA",
        "PAGTO DE FATURA",
        "PAGAMENTO FATURA",
        "PAGTO FATURA",
        "PAGAMENTO CARTAO DE CREDITO",
        "PAGAMENTO CARTÃO DE CRÉDITO",
        "PAGAMENTO DE CARTAO",
        "PAGAMENTO DE CARTÃO"
    ];

    public static bool IsBillPayment(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var normalized = description.ToUpperInvariant();
        return BillPaymentKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal));
    }
}
