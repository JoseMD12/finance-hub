namespace FinanceHub.PluggyIntegration.Infrastructure.Clients;

public interface IPluggyHttpExecutor
{
    Task<TResponse> GetAsync<TResponse>(string endpoint, string accessToken, CancellationToken cancellationToken = default);
}
