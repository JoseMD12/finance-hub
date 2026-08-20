namespace FinanceHub.Shared.Observability.Security;

public interface IEnvelopeEncryptionService
{
    string Encrypt(string plainText, byte[] masterKey);
    string Decrypt(string cipherText, byte[] masterKey);
}
