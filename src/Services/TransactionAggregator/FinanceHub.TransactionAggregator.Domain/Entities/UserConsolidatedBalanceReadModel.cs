namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class UserConsolidatedBalanceReadModel
{
    public string UserId { get; private set; } = string.Empty;
    public decimal TotalCheckingBalance { get; private set; }
    public decimal TotalCreditCardSpent { get; private set; }
    public decimal NetConsolidatedBalance { get; private set; }
    public DateTime LastCalculatedAtUtc { get; private set; }

    private UserConsolidatedBalanceReadModel() { }

    public UserConsolidatedBalanceReadModel(
        string userId,
        decimal totalCheckingBalance,
        decimal totalCreditCardSpent)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("O ID do usuário é obrigatório.", nameof(userId));

        UserId = userId;
        TotalCheckingBalance = totalCheckingBalance;
        TotalCreditCardSpent = totalCreditCardSpent;
        NetConsolidatedBalance = totalCheckingBalance - totalCreditCardSpent;
        LastCalculatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateBalance(decimal totalCheckingBalance, decimal totalCreditCardSpent)
    {
        TotalCheckingBalance = totalCheckingBalance;
        TotalCreditCardSpent = totalCreditCardSpent;
        NetConsolidatedBalance = totalCheckingBalance - totalCreditCardSpent;
        LastCalculatedAtUtc = DateTime.UtcNow;
    }
}
