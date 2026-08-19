using FinanceHub.PluggyIntegration.Domain.Entities;

namespace FinanceHub.PluggyIntegration.Domain.Aggregates;

public sealed class PluggySyncSessionAggregate
{
    public Guid SessionId { get; }
    public string UserId { get; }
    public string SourceName { get; }

    private readonly Dictionary<string, PluggyAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PluggyTransaction> _transactions = [];

    public IReadOnlyCollection<PluggyAccount> Accounts => _accounts.Values;
    public IReadOnlyCollection<PluggyTransaction> Transactions => _transactions.AsReadOnly();

    private PluggySyncSessionAggregate(string userId, string sourceName)
    {
        SessionId = Guid.NewGuid();
        UserId = string.IsNullOrWhiteSpace(userId) ? throw new ArgumentException("UserId é obrigatório.", nameof(userId)) : userId;
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? "Meu.Pluggy" : sourceName;
    }

    public static PluggySyncSessionAggregate Create(string userId, string sourceName)
    {
        return new PluggySyncSessionAggregate(userId, sourceName);
    }

    public PluggyAccount RegisterAccount(
        string accountId,
        string type,
        string? subtype,
        string name,
        decimal balance,
        string? currencyCode,
        string? rawBalanceDueDate)
    {
        if (_accounts.TryGetValue(accountId, out var existing))
        {
            return existing;
        }

        var account = new PluggyAccount(accountId, type, subtype, name, balance, currencyCode, rawBalanceDueDate);
        _accounts[accountId] = account;
        return account;
    }

    public PluggyTransaction RecordTransaction(
        string txId,
        string description,
        decimal amount,
        string rawDate,
        string? category,
        string accountId)
    {
        var tx = new PluggyTransaction(txId, description, amount, rawDate, category, accountId);
        _transactions.Add(tx);
        return tx;
    }
}
