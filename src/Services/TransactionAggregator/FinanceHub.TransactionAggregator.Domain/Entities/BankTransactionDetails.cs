namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class BankTransactionDetails
{
    public string BankTransactionId { get; private set; }
    public TransactionChannel Channel { get; private set; }
    public string MerchantName { get; private set; }

    private BankTransactionDetails()
    {
        BankTransactionId = string.Empty;
        MerchantName = string.Empty;
    }

    public BankTransactionDetails(string bankTransactionId, TransactionChannel channel, string merchantName)
    {
        BankTransactionId = bankTransactionId ?? string.Empty;
        Channel = channel;
        MerchantName = merchantName ?? string.Empty;
    }
}
