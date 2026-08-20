using System.Security.Cryptography;
using FinanceHub.Shared.Observability.Security;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Unit.Shared;

[Trait("Category", "Unit")]
public class EnvelopeEncryptionServiceTests
{
    private readonly EnvelopeEncryptionService _service = new();
    private readonly byte[] _validMasterKey = RandomNumberGenerator.GetBytes(32);

    [Fact]
    public void EncryptAndDecrypt_ShouldReturnOriginalPlainText_WhenKeyIsValid()
    {
        // Arrange
        var originalText = "Dados_Sensiveis_LGPD_Mock_Payload_Para_Validacao_AES256GCM";

        // Act
        var encrypted = _service.Encrypt(originalText, _validMasterKey);
        var decrypted = _service.Decrypt(encrypted, _validMasterKey);

        // Assert
        encrypted.Should().NotBeNullOrWhiteSpace();
        encrypted.Should().NotBe(originalText);
        decrypted.Should().Be(originalText);
    }

    [Fact]
    public void Encrypt_ShouldThrowArgumentException_WhenKeyLengthIsNot256Bits()
    {
        // Arrange
        var invalidKey = new byte[16];

        // Act
        var act = () => _service.Encrypt("test", invalidKey);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*256 bits*");
    }

    [Fact]
    public void Decrypt_ShouldThrowCryptographicException_WhenKeyIsIncorrect()
    {
        // Arrange
        var text = "Dados sensíveis LGPD";
        var encrypted = _service.Encrypt(text, _validMasterKey);
        var wrongKey = RandomNumberGenerator.GetBytes(32);

        // Act
        var act = () => _service.Decrypt(encrypted, wrongKey);

        // Assert
        act.Should().Throw<CryptographicException>();
    }
}
