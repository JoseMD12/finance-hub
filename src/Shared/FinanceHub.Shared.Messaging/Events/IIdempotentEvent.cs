namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Identifies an event with a unique message hash for inbox deduplication and idempotent processing.
/// </summary>
public interface IIdempotentEvent : IFinanceHubEvent
{
    string MessageHash { get; }
}
