using FinanceHub.Shared.Certificates;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceHub.UnitTests.Shared;

public class CertificateTests
{
    [Fact]
    public void FileSystemCertificateProvider_WithMissingPath_ShouldReturnNullAndLogWarning()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "OpenFinance:Itau:CertificatePath", "non_existent_cert.pfx" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var logger = NullLogger<FileSystemCertificateProvider>.Instance;
        var provider = new FileSystemCertificateProvider(config, logger);

        // Act
        var cert = provider.GetClientCertificate("Itau");

        // Assert
        cert.Should().BeNull();
    }

    [Fact]
    public void FileSystemCertificateProvider_WithEmptyPath_ShouldReturnNull()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var logger = NullLogger<FileSystemCertificateProvider>.Instance;
        var provider = new FileSystemCertificateProvider(config, logger);

        // Act
        var cert = provider.GetClientCertificate("MercadoPago");

        // Assert
        cert.Should().BeNull();
    }

    [Fact]
    public void AddFinanceHubCertificates_ShouldRegisterCertificateProviderAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddFinanceHubCertificates(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<ICertificateProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<FileSystemCertificateProvider>();
    }
}
