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
    /// <summary>
    /// <c>null</c> significa "o publisher não informou" — mensagem de uma versão anterior, ou já
    /// enfileirada antes do deploy. É diferente de <c>false</c>, que afirma não ser cartão.
    /// Sem essa distinção, um deploy parcial apagaria limites e vencimentos já sincronizados.
    /// </summary>
    bool? IsCreditCard = null,
    decimal? CreditLimit = null,
    decimal? AvailableCreditLimit = null,
    DateTime? InvoiceDueDateUtc = null,
    DateTime? InvoiceClosingDateUtc = null);

public record AccountBalanceSnapshotSynchronized(
    string UserId,
    IReadOnlyList<AccountBalanceSnapshotItem> Accounts,
    DateTime SynchronizedAtUtc) : IFinanceHubEvent;
