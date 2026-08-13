using System;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public readonly record struct CanonicalTransactionCreationParams(
    string UserId,
    AccountIdentifier AccountInfo,
    TransactionHash Hash,
    Money Amount,
    TransactionType Type,
    SanitizedDescription Description,
    Guid CategoryId,
    CategorizationSource CategorizationSource,
    DateTime TransactionDateUtc,
    BankTransactionDetails BankDetails);
