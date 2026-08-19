using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.PluggyIntegration.Infrastructure.Clients;

public interface IPluggyHttpExecutor
{
    Task<TResponse> GetAsync<TResponse>(string endpoint, string accessToken, CancellationToken cancellationToken = default);
    Task<TResponse> PatchAsync<TResponse>(string endpoint, object body, string accessToken, CancellationToken cancellationToken = default);
}
