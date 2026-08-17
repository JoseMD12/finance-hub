namespace FinanceHub.PluggyIntegration.Domain.Constants;

public static class PluggyCategoryMapper
{
    private static readonly Dictionary<string, string> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Transfer - PIX", "Transferência" },
        { "Transfer - TED", "Transferência" },
        { "Transfer - TEF", "Transferência" },
        { "Transfer", "Transferência" },
        { "Proceeds interests and dividends", "Rendimentos" },
        { "Investment", "Investimentos" },
        { "Eating out", "Alimentação" },
        { "Groceries", "Supermercado" },
        { "Parking", "Transporte" },
        { "Transport", "Transporte" },
        { "Fuel", "Transporte" },
        { "Clothing", "Vestuário" },
        { "Digital services", "Serviços Digitais" },
        { "Credit card payment", "Pagamento de Fatura" },
        { "Services", "Serviços" },
        { "Education", "Educação" },
        { "Health", "Saúde" },
        { "Pharmacy", "Farmácia" },
        { "Entertainment", "Entretenimento" },
        { "Home", "Moradia" }
    };

    public static string Map(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
            return "Outros";

        return CategoryMap.TryGetValue(rawCategory.Trim(), out var mapped)
            ? mapped
            : "Outros";
    }
}
