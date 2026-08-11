namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted by AuthConsent service when a new bank account consent is granted by the user.
/// </summary>
public record BankAccountLinked(
    Guid LinkId,
    string InstitutionId,
    string UserId,
    string ConsentId,
    DateTime LinkedAtUtc
);
