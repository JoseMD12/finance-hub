using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceHub.Shared.Certificates;

public class FileSystemCertificateProvider : ICertificateProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileSystemCertificateProvider> _logger;

    public FileSystemCertificateProvider(
        IConfiguration configuration,
        ILogger<FileSystemCertificateProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public X509Certificate2? GetClientCertificate(string institutionId)
    {
        var section = _configuration.GetSection($"OpenFinance:{institutionId}");
        var certPath = section["CertificatePath"];
        var certPassword = section["CertificatePassword"];

        if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
        {
            _logger.LogWarning("No mTLS certificate found for institution {InstitutionId} at path '{CertPath}'. Using Dev Fallback Mock.", institutionId, certPath);
            return null;
        }

        try
        {
            _logger.LogInformation("Loading mTLS certificate for institution {InstitutionId} from '{CertPath}'", institutionId, certPath);
            return X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mTLS certificate for institution {InstitutionId}", institutionId);
            return null;
        }
    }
}
