using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceHub.MercadoPagoIntegration.Domain.Constants;
using FinanceHub.Shared.Connectors;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Mapping;

public static partial class PluggyMappingProfile
{
    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b", RegexOptions.Compiled)]
    private static partial Regex CpfRegex();

    [GeneratedRegex(@"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b", RegexOptions.Compiled)]
    private static partial Regex CnpjRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public static string SanitizePii(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return rawJson;
        }

        var result = CpfRegex().Replace(rawJson, "***.***.***-**");
        result = CnpjRegex().Replace(result, "**.***.***/****-**");
        result = EmailRegex().Replace(result, "***@***.***");

        return result;
    }

    public static BankAccountDto ToBankAccountDto(JsonElement element)
    {
        var id = element.GetProperty("id").GetString() ?? "";
        var name = element.TryGetProperty("name", out var n) ? n.GetString() ?? "Conta Mercado Pago" : "Conta Mercado Pago";
        var number = element.TryGetProperty("number", out var num) ? num.GetString() : null;
        var currency = element.TryGetProperty("currencyCode", out var c) ? c.GetString() ?? "BRL" : "BRL";
        var type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "PAYMENT" : "PAYMENT";

        return new BankAccountDto(
            AccountId: id,
            BankIdentifier: MercadoPagoConstants.BankIdentifier,
            AccountType: type,
            Currency: currency,
            Nickname: string.IsNullOrWhiteSpace(number) ? name : $"{name} ({number})"
        );
    }

    public static BankTransactionDto ToBankTransactionDto(JsonElement element, string accountId)
    {
        var id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
        var description = element.TryGetProperty("description", out var desc) ? desc.GetString() ?? "Movimentação Open Finance" : "Movimentação Open Finance";
        var amount = element.TryGetProperty("amount", out var a) ? a.GetDecimal() : 0m;
        var currency = element.TryGetProperty("currencyCode", out var c) ? c.GetString() ?? "BRL" : "BRL";
        var dateStr = element.TryGetProperty("date", out var d) ? d.GetString() : null;
        var bookingDate = DateTimeOffset.TryParse(dateStr, out var dt) ? dt : DateTimeOffset.UtcNow;

        var type = element.TryGetProperty("type", out var ty) ? ty.GetString() ?? "DEBIT" : "DEBIT";
        var indicator = type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase) ? "CRDT" : "DBIT";

        // Convenção de sinal: Débito é negativo (-), Crédito é positivo (+)
        var signedAmount = indicator == "DBIT" ? -Math.Abs(amount) : Math.Abs(amount);

        var sanitizedPayload = SanitizePii(element.GetRawText());

        return new BankTransactionDto(
            TransactionId: id,
            AccountId: accountId,
            Amount: signedAmount,
            Currency: currency,
            BookingDateTime: bookingDate,
            TransactionInformation: description,
            CreditDebitIndicator: indicator,
            FeeAmount: null,
            RawPayload: sanitizedPayload
        );
    }
}
