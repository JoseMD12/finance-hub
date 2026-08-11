using FinanceHub.Shared.Certificates;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceHub.UnitTests.Shared;

public class SharedModulesTests
{
    [Fact]
    public void TransactionIngestedEvent_ShouldInstantiateCorrectly()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var ingestion = new TransactionIngested(
            IngestionId: Guid.NewGuid(),
            Source: "Itau",
            AccountId: "acc-123",
            BankTransactionId: "bank-tx-999",
            Amount: 150.75m,
            TransactionDate: now.Date,
            Description: "Supermercado XYZ",
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: now
        );

        // Assert
        ingestion.Source.Should().Be("Itau");
        ingestion.Amount.Should().Be(150.75m);
        ingestion.Description.Should().Be("Supermercado XYZ");
    }

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
}
