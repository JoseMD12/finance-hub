using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.Shared.Certificates;

public static class CertificateExtensions
{
    public static IServiceCollection AddFinanceHubCertificates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICertificateProvider, FileSystemCertificateProvider>();
        return services;
    }
}
