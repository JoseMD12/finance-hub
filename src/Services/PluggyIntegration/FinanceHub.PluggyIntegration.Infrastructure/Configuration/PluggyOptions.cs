namespace FinanceHub.PluggyIntegration.Infrastructure.Configuration;

public class PluggyOptions
{
    public const string SectionName = "Pluggy";

    public string ApiBaseUrl { get; set; } = "https://my-api.pluggy.ai";
    public string UserToken { get; set; } = string.Empty;
}
