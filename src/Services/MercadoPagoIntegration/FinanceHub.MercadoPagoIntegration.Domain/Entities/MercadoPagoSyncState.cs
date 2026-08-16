using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

namespace FinanceHub.MercadoPagoIntegration.Domain.Entities;

public enum SyncExecutionStatus
{
    Idle = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

public class MercadoPagoSyncState
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string AccountId { get; private set; }
    public DateTime LastSyncCursorUtc { get; private set; }
    public DateTime LastExecutionUtc { get; private set; }
    public SyncExecutionStatus Status { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public int IngestedCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private MercadoPagoSyncState()
    {
        UserId = null!;
        AccountId = null!;
    }

    public static MercadoPagoSyncState Create(
        string userId,
        string accountId,
        DateTime initialCursorUtc,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new NullOrEmptyMercadoPagoCredentialsDomainException("UserId");

        if (string.IsNullOrWhiteSpace(accountId))
            throw new NullOrEmptyMercadoPagoCredentialsDomainException("AccountId");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new MercadoPagoSyncState
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = accountId,
            LastSyncCursorUtc = initialCursorUtc,
            LastExecutionUtc = now,
            Status = SyncExecutionStatus.Idle,
            IngestedCount = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void StartSync(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        Status = SyncExecutionStatus.InProgress;
        LastExecutionUtc = now;
        LastErrorMessage = null;
        UpdatedAtUtc = now;
    }

    public void CompleteSync(DateTime newCursorUtc, int ingestedInBatch, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (newCursorUtc > LastSyncCursorUtc)
        {
            LastSyncCursorUtc = newCursorUtc;
        }

        Status = SyncExecutionStatus.Completed;
        IngestedCount += ingestedInBatch;
        UpdatedAtUtc = now;
    }

    public void FailSync(string errorMessage, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        Status = SyncExecutionStatus.Failed;
        LastErrorMessage = errorMessage;
        UpdatedAtUtc = now;
    }
}
