namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Configuration;

public class OpenFinanceOptions
{
    public const string SectionName = "OpenFinance";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.pluggy.ai";
    public int TimeoutSeconds { get; set; } = 30;
}
