# Database Guidelines: PostgreSQL & Entity Framework Core

This document defines database design, modeling, and Entity Framework Core usage standards for **FinanceHub** targeting PostgreSQL 16+.

---

## 1. Financial Transaction Modeling & Double-Entry Ledger
Financial integrity requires auditability, immutability, and deterministic accounting.

### Guidelines
* **Immutable Ledger Entries**: Transaction history records are strictly append-only. Never perform SQL `UPDATE` or `DELETE` on posted ledger entries.
* **Double-Entry Balance Verification**: Every financial transaction consists of balanced credit and debit entries ($\sum \text{Debits} = \sum \text{Credits}$).
* **Explicit State Transitions**: Manage transaction lifecycle using a strict state machine (`Draft` $\rightarrow$ `Pending` $\rightarrow$ `Settled` | `Failed` | `Reversed`). State transitions must be atomic.

```csharp
// Entity Framework Configuration for Financial Transaction
public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("financial_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status)
               .HasConversion<string>()
               .HasMaxLength(32)
               .IsRequired();

        // Enforce concurrency token for state updates
        builder.Property(t => t.RowVersion)
               .IsRowVersion();
    }
}
```

---

## 2. High-Precision Currency & Money Handling
Floating-point types (`float`, `double`) introduce rounding errors and must **NEVER** be used for financial amounts.

### Guidelines
* **Column Types**: Always map monetary values to PostgreSQL `numeric(18,4)` (or `numeric(18,2)` where regulatory minimum decimal precision permits).
* **ISO Currency Code**: Store currency explicitly alongside amounts as a 3-character ISO 4217 string (e.g., `"BRL"`, `"USD"`).
* **Money Value Object**: Map `Money` value objects using EF Core Owned Types or Complex Types (C# 12+).

```csharp
public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ComplexProperty(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("amount")
                 .HasColumnType("numeric(18,4)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("currency")
                 .HasMaxLength(3)
                 .IsFixedLength()
                 .IsRequired();
        });
    }
}
```

---

## 3. Idempotency & Transaction Deduplication Indexes
Prevent duplicate payments and race conditions under high throughput.

### Guidelines
* **Idempotency Key Unique Index**: Require an `Idempotency-Key` (UUIDv7 or SHA-256 hash of client request payload) on all mutating endpoints.
* **Database Level Constraints**: Enforce uniqueness directly in PostgreSQL via unique composite indexes.

```csharp
builder.HasIndex(t => t.IdempotencyKey)
       .IsUnique()
       .HasDatabaseName("ix_financial_transactions_idempotency_key");

builder.HasIndex(t => new { t.SourceAccountId, t.ExternalReferenceId })
       .IsUnique()
       .HasDatabaseName("ix_financial_transactions_account_ext_ref");
```

---

## 4. Async EF Core Query Optimization
Avoid performance degradation, memory bloat, and query blocking.

### Mandatory Rules
1. **`AsNoTracking()`**: Use `.AsNoTracking()` for all read-only queries.
2. **Avoid N+1 Problems**: Use `.Include()` / `.ThenInclude()` intentionally or project directly onto DTOs using `.Select()`.
3. **`AsSplitQuery()`**: Apply `.AsSplitQuery()` when fetching multi-collection includes to prevent Cartesian explosive joins.
4. **Compiled Queries**: Use `EF.CompileAsyncQuery` for frequently invoked hot-path queries.

```csharp
// Optimized Direct DTO Projection Query
public async Task<List<TransactionSummaryDto>> GetRecentAccountTransactionsAsync(
    Guid accountId, 
    int limit, 
    CancellationToken ct)
{
    return await dbContext.FinancialTransactions
        .AsNoTracking()
        .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
        .OrderByDescending(t => t.CreatedAtUtc)
        .Take(limit)
        .Select(t => new TransactionSummaryDto(
            t.Id,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Status.ToString(),
            t.CreatedAtUtc))
        .ToListAsync(ct);
}
```

---

## 5. Safe Zero-Downtime Database Migrations
Migrations in production must never cause table locks or downtime.

### Guidelines
* **Non-Blocking Index Creation**: Use `CREATE INDEX CONCURRENTLY` in raw migration SQL for existing large tables.
* **Separation of Schema and Data**: Never perform heavy data transformations inside EF Core `Migration.Up()` C# methods. Execute data migrations in isolated worker scripts.
* **Backward-Compatible Changes**: 
  * Phase 1: Add new columns as nullable.
  * Phase 2: Deploy code writing to both old and new columns.
  * Phase 3: Backfill historical data.
  * Phase 4: Drop old column in a subsequent deployment.
* **Rollback Script Verification**: Test every migration's `Down()` path or rollback script before merging PRs.
