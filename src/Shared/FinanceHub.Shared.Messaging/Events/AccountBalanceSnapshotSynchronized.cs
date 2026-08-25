using System;
using System.Collections.Generic;

namespace FinanceHub.Shared.Messaging.Events;

public record AccountBalanceSnapshotItem(
    string AccountId,
    string InstitutionId,
    string AccountType,
    decimal CurrentBalance,
    string Currency,
    DateTime SnapshotAtUtc);

public record AccountBalanceSnapshotSynchronized(
    string UserId,
    IReadOnlyList<AccountBalanceSnapshotItem> Accounts,
    DateTime SynchronizedAtUtc) : IFinanceHubEvent;
