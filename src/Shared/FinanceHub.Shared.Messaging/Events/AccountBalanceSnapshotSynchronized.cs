using System;
using System.Collections.Generic;

namespace FinanceHub.Shared.Messaging.Events;

public record AccountBalanceSnapshotItem(
    string AccountId,
    string InstitutionId,
    string AccountType,
    decimal CurrentBalance,
    string Currency,
    DateTime SnapshotAtUtc,
    bool IsCreditCard = false,
    decimal? CreditLimit = null,
    decimal? AvailableCreditLimit = null,
    DateTime? InvoiceDueDateUtc = null,
    DateTime? InvoiceClosingDateUtc = null);

public record AccountBalanceSnapshotSynchronized(
    string UserId,
    IReadOnlyList<AccountBalanceSnapshotItem> Accounts,
    DateTime SynchronizedAtUtc) : IFinanceHubEvent;
