namespace FinanceHub.TransactionAggregator.Domain.Entities;

public enum TransactionType
{
    Credit = 1,
    Debit = 2
}

public enum CategorizationSource
{
    UserManual = 1,
    UserRule = 2,
    GlobalRule = 3,
    Fallback = 4
}

public enum TransactionChannel
{
    Pix = 1,
    Ted = 2,
    Doc = 3,
    CreditCard = 4,
    DebitCard = 5,
    BankTransfer = 6,
    Other = 99
}
