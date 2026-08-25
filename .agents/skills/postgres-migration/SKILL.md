---
name: postgres-migration
description: Safe EF Core Migrations workflow for FinanceHub PostgreSQL databases, covering migration creation, SQL script generation and validation, zero-downtime schema updates, lock timeout safety, and rollback procedures.
---

# PostgreSQL Safe EF Core Migration Guide

This guide establishes the mandatory workflow for schema modifications in **FinanceHub** using EF Core 9/10 and PostgreSQL (Npgsql). Each microservice in FinanceHub manages its own PostgreSQL database under **Database-per-Service** isolation. Financial data safety, zero-downtime operations, and deterministic rollbacks are non-negotiable.

---

## 1. Core Principles for Financial Schema Updates

1. **Database-per-Service Isolation**: Migrations target only the specific microservice database (e.g., `FinanceHub.PluggyIntegration` or `FinanceHub.TransactionAggregator`). Cross-service database modifications are strictly prohibited.
2. **Zero Data Loss**: Never drop columns or tables directly in production without a deprecation phase.
3. **Zero Downtime**: DDL operations must never hold exclusive table locks (`ACCESS EXCLUSIVE`) for long periods.
4. **Idempotency**: All SQL migration scripts produced for deployment must be idempotent (`IF NOT EXISTS`).
5. **Pre-Deployment SQL Inspection**: Generated SQL must be reviewed manually or via CI before applying to Staging/Production.

---

## 2. Step 1: Creating EF Core Migrations

Run migration commands targeting the specific microservice project (e.g., `src/Services/FinanceHub.TransactionAggregator`):

### Command Standard:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator \
  --output-dir Migrations
```

### Naming Convention:
Name migrations with action and subject in PascalCase:
- `AddReconciliationBatchesTable`
- `AddTransactionHashUnicoIndex`
- `MakeAccountMetadataNullable`

---

## 3. Step 2: Generating & Inspecting SQL Scripts

Before applying migrations, generate the idempotent SQL script:

```bash
dotnet ef migrations script \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator \
  --idempotent \
  --output bin/migrations/<MigrationName>.sql
```

### SQL Safety Inspection Checklist:
- [ ] **Lock Timeout**: Does the script set a short lock timeout to avoid blocking active web worker connections?
  ```sql
  SET lock_timeout = '5s';
  ```
- [ ] **No Rewrite Constraints**: Are new columns added as `NULL` or with `DEFAULT` (PostgreSQL 11+ metadata-only)?
- [ ] **Concurrent Index Creation**: Are new indexes created using `CREATE INDEX CONCURRENTLY`?

---

## 4. Step 3: Zero-Downtime PostgreSQL Patterns

### Pattern A: Creating Indexes Safely (`CONCURRENTLY`)
Standard `CREATE INDEX` acquires a `SHARE` lock blocking concurrent writes (`INSERT`, `UPDATE`, `DELETE`). Always use concurrent index creation.

**EF Core Builder Annotation**:
```csharp
builder.Entity<Transaction>()
    .HasIndex(t => new { t.AccountId, t.TransactionDate })
    .HasDatabaseName("IX_Transactions_AccountId_TransactionDate")
    .IsCreatedConcurrently();
```

**Generated SQL Verification**:
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Transactions_AccountId_TransactionDate" 
ON "Transactions" ("AccountId", "TransactionDate");
```

> **Note**: `CREATE INDEX CONCURRENTLY` cannot run inside a multi-statement transaction block. Ensure EF Core migration marks transaction usage appropriately or manually splits DDL steps.

### Pattern B: Adding Non-Nullable Columns
1. **Phase 1 (Migration N)**: Add column as `NULLABLE`.
2. **Phase 2 (App Release)**: Deploy app code writing to both old and new schema. Backfill existing records in background batch.
3. **Phase 3 (Migration N+1)**: Add `NOT NULL` constraint once 100% of rows are populated.

### Pattern C: Renaming Columns or Tables
1. **Phase 1**: Add new column alongside old column.
2. **Phase 2**: Dual write to both columns via Application layer or PostgreSQL Trigger.
3. **Phase 3**: Backfill historic data from old column to new column.
4. **Phase 4**: Switch read traffic to new column.
5. **Phase 5**: Drop old column in a subsequent maintenance cycle.

---

## 5. Step 4: Applying Migrations Across Environments

### Local Development:
```bash
dotnet ef database update \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator
```

### CI/CD Pipeline & Production Deployment:
Use `dotnet ef` migration bundles to compile standalone deployment binaries per microservice:

```bash
# Build standalone migration executable bundle
dotnet ef migrations bundle \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator \
  --output bin/efbundle \
  --self-contained -r linux-x64

# Execute in production deployment pipeline for target microservice database:
./bin/efbundle --connection "$TRANSACTION_AGGREGATOR_DB_CONNECTION"
```

---

## 6. Step 5: Rollback & Recovery Plan

Every schema change must have a tested, verified rollback strategy.

### 1. Rolling Back local / staging environment:
```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator
```

### 2. Generating Down SQL Scripts:
```bash
dotnet ef migrations script <TargetMigrationName> <PreviousMigrationName> \
  --project src/Services/FinanceHub.TransactionAggregator \
  --startup-project src/Services/FinanceHub.TransactionAggregator \
  --output bin/migrations/rollback_<TargetMigrationName>.sql
```

### 3. Emergency Safety Checklist:
1. Verify database point-in-time recovery (PITR) / WAL backups are active before running migrations on production.
2. If `lock_timeout` expires during execution, do **NOT** force locks without checking long-running queries in `pg_stat_activity`.
3. Verify table bloat and index validity after rollback using:
   ```sql
   SELECT indexrelname, indisvalid FROM pg_index i 
   JOIN pg_class c ON c.oid = i.indexrelid 
   WHERE indisvalid = false;
   ```

