namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class InboxProcessedMessage
{
    public string MessageHash { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; private set; }

    private InboxProcessedMessage() { }

    public InboxProcessedMessage(string messageHash, string eventType)
    {
        if (string.IsNullOrWhiteSpace(messageHash))
            throw new ArgumentException("Hash da mensagem é obrigatório para deduplicação.", nameof(messageHash));

        MessageHash = messageHash;
        EventType = string.IsNullOrWhiteSpace(eventType) ? "Unknown" : eventType;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
