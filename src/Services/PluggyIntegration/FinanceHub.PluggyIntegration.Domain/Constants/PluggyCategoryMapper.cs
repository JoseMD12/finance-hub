namespace FinanceHub.PluggyIntegration.Domain.Constants;

public static class PluggyCategoryMapper
{
    private const string Transferencia = "Transferência";
    private const string Rendimentos = "Rendimentos";
    private const string Investimentos = "Investimentos";
    private const string Alimentacao = "Alimentação";
    private const string Supermercado = "Supermercado";
    private const string Transporte = "Transporte";
    private const string Vestuario = "Vestuário";
    private const string ServicosDigitais = "Serviços Digitais";
    private const string PagamentoFatura = "Pagamento de Fatura";
    private const string Servicos = "Serviços";
    private const string Educacao = "Educação";
    private const string Saude = "Saúde";
    private const string Farmacia = "Farmácia";
    private const string Entretenimento = "Entretenimento";
    private const string Moradia = "Moradia";
    private const string Outros = "Outros";

    private static readonly Dictionary<string, string> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Transfer - PIX", Transferencia },
        { "Transfer - TED", Transferencia },
        { "Transfer - TEF", Transferencia },
        { "Transfer", Transferencia },
        { "Proceeds interests and dividends", Rendimentos },
        { "Investment", Investimentos },
        { "Eating out", Alimentacao },
        { "Groceries", Supermercado },
        { "Parking", Transporte },
        { "Transport", Transporte },
        { "Fuel", Transporte },
        { "Clothing", Vestuario },
        { "Digital services", ServicosDigitais },
        { "Credit card payment", PagamentoFatura },
        { "Services", Servicos },
        { "Education", Educacao },
        { "Health", Saude },
        { "Pharmacy", Farmacia },
        { "Entertainment", Entretenimento },
        { "Home", Moradia }
    };

    public static string Map(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
            return Outros;

        return CategoryMap.TryGetValue(rawCategory.Trim(), out var mapped)
            ? mapped
            : Outros;
    }
}
