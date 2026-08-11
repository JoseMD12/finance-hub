using System.Security.Cryptography.X509Certificates;

namespace FinanceHub.Shared.Certificates;

public interface ICertificateProvider
{
    /// <summary>
    /// Obtains the mTLS client certificate for the specified financial institution.
    /// Returns null if running in Dev fallback mode without a certificate file.
    /// </summary>
    X509Certificate2? GetClientCertificate(string institutionId);
}
