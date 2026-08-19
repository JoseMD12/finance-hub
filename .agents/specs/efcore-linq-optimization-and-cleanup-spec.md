# 📐 Technical Specification: EF Core Query Optimizations, Cleanup & CI/CD Modernization

> **Document:** `.agents/specs/efcore-linq-optimization-and-cleanup-spec.md`  
> **Status:** 🟢 `Approved & In Execution`  
> **Date:** 2026-08-19  

---

## 1. 🎯 Scope & Objectives

1. **EF Core LINQ Query Optimization (TransactionAggregator)**:
   - Eliminate redundant database roundtrips in CQRS command/query pipelines (e.g. `ExistsByHashAsync` + `GetIdByHashAsync` combined into a single query).
   - Implement direct LINQ projections (`.AsNoTracking().Select(...)`) in read queries (`GetTransactionsQueryHandler`, `GetConsolidatedBalanceQueryHandler`) to select only required columns and avoid entity tracking overhead.
   - Optimize state tracking in repositories (`AccountBalanceRepository`, `UserCategoryRuleRepository`) using EF Core entry tracking state (`EntityState.Detached`) instead of issuing duplicate `SELECT` queries before saving.
   - Completely remove in-memory EF Core package references (`Microsoft.EntityFrameworkCore.InMemory`) across the solution.
2. **Technical Debt & Repository Cleanup**:
   - Delete empty decommissioned service directories: `src/Services/AuthConsent`, `src/Services/InterIntegration`, `src/Services/ItauIntegration`, `src/Services/MercadoPagoIntegration`, and `src/Shared/FinanceHub.Shared.Certificates`.
   - **Preserve** `extensions/` folder for later manual handling as instructed by the user.
   - Harmonize EF Core (9.0.2), Npgsql (9.0.2), and MassTransit (8.4.1) package versions to eliminate `MSB3277` compiler warnings.
   - Resolve package vulnerability warnings (`NU1902`, `NU1903`) in OpenTelemetry and SSH.NET.
   - Fix xUnit nullable parameter warning `xUnit1012` in `SyncAllPluggyAccountsCommandHandlerTests.cs`.
3. **CI/CD Pipeline Modernization**:
   - Update `.github/workflows/ci-build-test.yml` to remove decommissioned `AUTH_CONSENT_BASE_URL` and configure active services (`PLUGGY_INTEGRATION_BASE_URL`).
   - Add parallel frontend validation jobs (`Node 20`, `npm ci`, build & lint for `FinanceHub.Web` and `FinanceHub.Web.Extension`).

---

## 2. 🏛️ Architectural Decisions

- **Decision 1: CQRS Read Query Data Access Strategy**:
  - **Chosen Strategy**: Dedicated Repository Query Methods with Direct DTO Projection (`GetProjectedByUserIdAsync` in `ITransactionRepository` and `GetProjectedByUserIdAsync` in `IAccountBalanceRepository`).
  - **Rationale**: Keeps `DbContext` encapsulated within `Infrastructure` layer while leveraging EF Core's SQL translation to project directly into `TransactionDto` and `AccountBalanceDto` with `.AsNoTracking()`.
- **Decision 2: Ingestion & Repository State Tracking**:
  - **Chosen Strategy**: Single-Lookup (`GetIdByHashAsync`) & EF Core Entry State Checking (`_context.Entry(entity).State == EntityState.Detached`).
  - **Rationale**: Eliminates 2 redundant database SELECT roundtrips per transaction ingest without adding heavy unit-of-work abstractions.
- **Decision 3: CI/CD Pipeline Structure**:
  - **Chosen Strategy**: Parallel GitHub Actions Jobs (`backend`, `frontend-web`, `frontend-extension`).
  - **Rationale**: Runs .NET 10 compilation/tests and Node 20 frontend builds concurrently, cutting CI cycle time in half.

---

## 3. 💥 Impact Analysis Across the System

### 3.1 ⚙️ Backend Services Impact
- **`FinanceHub.TransactionAggregator`**:
  - **Query Performance**: `GET /api/v1/transactions` and `GET /api/v1/transactions/balances/user/{userId}` now execute single SQL queries selecting only mapped columns. Avoids instantiating 5+ Value Objects per row (`AccountIdentifier`, `Money`, `SanitizedDescription`, `BankTransactionDetails`, `TransactionAuditInfo`) and eliminates EF Core change tracking overhead.
  - **Ingestion Throughput**: `IngestTransactionCommandHandler` now performs 1 hash lookup instead of 2 (`Exists` + `GetId`), and `AccountBalanceRepository.AddOrUpdateAsync` no longer executes a redundant `SELECT` when updating tracked balance records.
  - **Dependencies**: Removed `Microsoft.EntityFrameworkCore.InMemory`. All unit and integration tests use Testcontainers (PostgreSQL + RabbitMQ) or mock interfaces.
- **`FinanceHub.ApiGateway`**:
  - Receives faster response times and reduced latency from downstream `TransactionAggregator` calls during dashboard aggregation (`GET /api/v1/gateway/dashboard`).
  - Decommissioned legacy route references to `AuthConsent`.
- **`FinanceHub.PluggyIntegration`**:
  - No contract changes. Test suite updated with nullable token parameters.

### 3.2 🌐 Frontend (`FinanceHub.Web`) & Browser Extension (`FinanceHub.Web.Extension`) Impact
- **Zero API Contract Breaking Changes**: All DTO properties and JSON field names in `TransactionDto`, `AccountBalanceDto`, and `ConsolidatedBalanceDto` remain 100% backward-compatible.
- **CI/CD Quality Gate**: Both `FinanceHub.Web` (React + Vite) and `FinanceHub.Web.Extension` (WXT) are now automatically built and validated in GitHub Actions on every push and pull request.

---

## 4. 🛡️ Warning Resolution Plan

| Warning Code | Root Cause | Resolution Strategy |
| :--- | :--- | :--- |
| **`MSB3277`** (EF Core Conflict) | Mismatch between EF Core 9.0.0 and 9.0.2 across projects. | Harmonize all `Microsoft.EntityFrameworkCore.*` and `Npgsql.*` dependencies to `9.0.2` in all `.csproj` files. |
| **`NU1902`** (OpenTelemetry Vulnerabilities) | `OpenTelemetry 1.11.1` had reported advisory vulnerabilities (GHSA-8785-wc3w-h8q6, GHSA-g94r-2vxg-569j, GHSA-4625-4j76-fww9). | Upgrade `OpenTelemetry` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` to `1.11.2` in `FinanceHub.Shared.Observability.csproj`. |
| **`NU1903`** (SSH.NET Vulnerability) | Testcontainers pulled transitive vulnerable dependency `SSH.NET 2023.0.0` (GHSA-q939-rpr3-3284). | Add explicit `SSH.NET 2024.1.0` package reference in `FinanceHub.UnitTests.csproj`. |
| **`xUnit1012`** (Test Null Parameter) | `[InlineData(null)]` used on non-nullable `string invalidToken` parameter. | Change test parameter type to `string? invalidToken` in `SyncAllPluggyAccountsCommandHandlerTests.cs`. |
| **`InMemory Reference`** | `Microsoft.EntityFrameworkCore.InMemory` was referenced in Infrastructure project. | Remove `Microsoft.EntityFrameworkCore.InMemory` package reference. |

---

## 5. 🧩 Interface & Implementation Contracts

### 5.1 `ITransactionRepository` Updates
```csharp
public interface ITransactionRepository
{
    Task<Guid?> GetIdByHashAsync(TransactionHash hash, CancellationToken cancellationToken);
    Task<CanonicalTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(CanonicalTransaction transaction, CancellationToken cancellationToken);
    Task UpdateAsync(CanonicalTransaction transaction, CancellationToken cancellationToken);
    Task<IEnumerable<TransactionDto>> GetProjectedByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IEnumerable<CanonicalTransaction>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
}
```

### 5.2 `IAccountBalanceRepository` Updates
```csharp
public interface IAccountBalanceRepository
{
    Task<AccountBalance?> GetByUserAndAccountAsync(string userId, AccountIdentifier accountInfo, CancellationToken cancellationToken);
    Task<IEnumerable<AccountBalanceDto>> GetProjectedByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<IEnumerable<AccountBalance>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task AddOrUpdateAsync(AccountBalance balance, CancellationToken cancellationToken);
}
```

---

## 6. 🧪 Testing & Validation Matrix

- [x] Unit tests for `TransactionRepository`, `AccountBalanceRepository`, and `UserCategoryRuleRepository`.
- [x] CQRS query handler tests verifying direct DTO projection and single-query execution.
- [x] `dotnet build --configuration Release` with 0 warnings.
- [x] `dotnet test --configuration Release` with 100% pass rate.
- [x] GitHub Actions CI workflow validates backend and frontend jobs.

---

## 7. ⚡ External API & Pipeline Performance Analysis: `SyncAllPluggyAccountsCommandHandler`

### 7.1 Identified Performance Bottleneck (3-Level Nested HTTP Loop & Sequential MassTransit Publish)

In `src/Services/PluggyIntegration/FinanceHub.PluggyIntegration.Application/Commands/SyncAllPluggyAccounts/SyncAllPluggyAccountsCommandHandler.cs`, the synchronization execution flow currently performs a **3-level nested sequential loop**:

```
Items (N) ──[HTTP 1]──> Accounts (M) ──[HTTP 2]──> Transactions (T) ──[Sequential MassTransit Publish]──> Outbox/RabbitMQ
```

#### Detailed Breakdown of Bottlenecks:
1. **$N \times M$ Sequential HTTP Roundtrips (Chatty External API)**:
   - For every item $i \in [1..N]$, it executes `GetAccountsByItemIdAsync` sequentially.
   - For every account $a \in [1..M]$, it executes `GetTransactionsByAccountIdAsync` sequentially.
   - Example: 3 bank connections with 2 accounts each = $1 + 3 + 6 = 10$ serial HTTP roundtrips to Pluggy API. Under network latency of ~150-300ms per request, this alone introduces 1.5s to 3s of idle waiting.
2. **Individual `await publishEndpoint.Publish(...)` Inside the Inner Loop ($T$ roundtrips)**:
   - For 100 transactions, `Publish` is awaited 100 separate times sequentially inside the loop (`foreach (var tx in transactions)`).
   - If MassTransit Outbox or RabbitMQ transaction is involved, this causes 100 individual sequential DB/broker roundtrips instead of a batch publish (`PublishBatch`).

---

### 7.2 Architectural Resolution & Optimization Plan

#### 1. Parallel Task Fan-Out with Rate Limiting (`Task.WhenAll` / `Parallel.ForEachAsync`)
- Instead of awaiting each account's transactions sequentially, parallelize account transaction fetching per item using `Task.WhenAll` or `Parallel.ForEachAsync(accounts, new ParallelOptions { MaxDegreeOfParallelism = 4 }, ...)`:
```csharp
// Fetch transactions for all accounts concurrently with controlled parallelism
var accountTasks = accounts.Select(async account =>
{
    var txs = await pluggyClient.GetTransactionsByAccountIdAsync(account.Id, command.PluggyAccessToken, cancellationToken);
    return (Account: account, Transactions: txs);
});
var accountResults = await Task.WhenAll(accountTasks);
```

#### 2. MassTransit Batch Event Publishing (`publishEndpoint.PublishBatch`)
- Transform transaction mapping into an in-memory collection and dispatch in a single atomic batch:
```csharp
var events = transactions.Select(tx => new TransactionIngested(...));
await publishEndpoint.PublishBatch(events, cancellationToken);
```
- Reduces $T$ serial broker/outbox roundtrips down to **1 single batch operation**.

---

### 7.3 Other Pipeline & Handler Inefficiencies Audited

| Component / Flow | Issue Description | Optimization Strategy |
| :--- | :--- | :--- |
| **`PluggyEndpoints.cs` (Sync Trigger)** | HTTP POST `/api/v1/pluggy/sync` blocks the client synchronously awaiting the entire multi-level sync loop to finish before returning `200 OK`. | Make the sync asynchronous: publish a `StartPluggySyncCommand` or background worker, returning `202 Accepted` immediately with a Job/Sync ID for large portfolios. |
| **`DashboardEndpoints.cs` in `ApiGateway`** | `GET /api/v1/gateway/dashboard` performs sequential HTTP calls: `GetConsolidatedBalanceAsync` followed by `GetTransactionsAsync`. | Use `Task.WhenAll(balanceTask, transactionsTask)` to fetch dashboard data concurrently from `TransactionAggregator`. |
| **`CategorizeTransactionCommandHandler.cs`** | Executes `GetByIdAsync`, modifies entity, `UpdateAsync`, then executes `ruleRepo.AddOrUpdateAsync` (multiple serial DB roundtrips). | Batch update or leverage EF Core `SaveChangesAsync` in a unified transactional scope. |

---

## 8. 🔮 Future Improvements Backlog (Post-Sprint Roadmap)

> **Note:** The following architectural enhancements are formally registered for implementation in a dedicated follow-up sprint:

1. **Optimistic Concurrency Control (`xmin` / RowVersion in EF Core & PostgreSQL)**:
   - **Target Entities**: `AccountBalance`, `CanonicalTransaction`, and `UserCategoryRule`.
   - **Mechanism**: Configure PostgreSQL system column `xmin` via `.IsRowVersion()` in EF Core entity type configurations to prevent lost updates under race conditions during high-frequency concurrent syncs and webhooks.
   - **Handling**: Handle `DbUpdateConcurrencyException` in application handlers with automated retry or merge policies.

2. **Dead Letter Queue (DLQ) & Fault Tolerance Infrastructure**:
   - **Target Consumers**: `TransactionIngestedConsumer`, `InvoiceItemIngestedConsumer`, and file parser ingestion workers.
   - **Mechanism**: Configure MassTransit Error / Dead Letter Queues (`_error` and `_skipped` exchanges/queues) with exponential backoff retry policies (`UseMessageRetry(r => r.Exponential(...))`) and Outbox compensation. Poison messages failing max retry thresholds will be routed to a dead-letter quarantine queue for SRE audit and re-drive capabilities.

3. **Chunked Parallel Batch Ingestion (`TransactionsBatchIngested` with 50 Items/Chunk)**:
   - **Target Flow**: `SyncAllPluggyAccountsCommandHandler` $\longrightarrow$ `TransactionsBatchIngestedConsumer` (in `TransactionAggregator`).
   - **Mechanism**: Group synchronized transactions into chunks of 50 items using `.Chunk(50)` and dispatch multiple `TransactionsBatchIngested` messages in parallel:
     ```csharp
     public record TransactionsBatchIngested(
         Guid BatchId,
         string UserId,
         int ChunkIndex,
         int TotalChunks,
         IReadOnlyList<TransactionIngestedItem> Transactions,
         DateTime OccurredAtUtc
     );
     ```
   - **Performance Gains**:
     - Reduces broker message overhead by a factor of 50 while preserving bounded message payload sizes.
     - Allows `TransactionAggregator` consumer to execute bulk hash lookups (`_context.Transactions.Where(t => batchHashes.Contains(t.Hash))`) and bulk inserts (`AddRangeAsync`), eliminating per-row EF Core transaction overhead while maintaining parallel worker scalability.

---

## 9. 🛠️ `PluggyIntegration` Service & Client Refactoring Specification

### 9.1 🎯 Scope & Objectives
1. **Single Responsibility Principle (SRP) Enforcement**:
   - Extract `IPluggyHttpExecutor` / `PluggyHttpExecutor` to encapsulate HTTP request creation, `Authorization: Bearer` header injection, HTTP status code resilience / exception translation (`401/403` $\rightarrow$ `PluggySessionExpiredDomainException`, `429` $\rightarrow$ `PluggyRateLimitDomainException`), and JSON deserialization.
2. **Resource API Client Simplification**:
   - Refactor `IMeuPluggyClient` and `MeuPluggyClient` to serve strictly as a low-level HTTP endpoint client (`GetItemsAsync`, `UpdateItemAsync`, `GetAccountsByItemIdAsync`, `GetTransactionsByAccountIdAsync`).
   - Remove high-level composite orchestration methods (`GetAllAccountsAsync`, `GetAllTransactionsAsync`) from the HTTP client interface and implementation.
3. **Application Aggregation Service**:
   - Introduce `IPluggyAggregationService` / `PluggyAggregationService` in `FinanceHub.PluggyIntegration.Application` to manage parallel Fan-Out fetching (`FetchAllAccountsAsync` and `FetchAllTransactionsAsync`).
4. **Mandatory Separate Files & Rule 13 Compliance**:
   - Interface (`IPluggyAggregationService.cs`) and implementation (`PluggyAggregationService.cs`) MUST reside in separate files in their respective folders (`Application/Interfaces/` and `Application/Services/`).
   - Interface (`IPluggyHttpExecutor.cs`) and implementation (`PluggyHttpExecutor.cs`) MUST reside in separate files in `Infrastructure/Clients/`.
5. **TDD Coverage**:
   - Execute Red-Green-Refactor cycle using xUnit, FluentAssertions, and NSubstitute, maintaining $\ge 80\%$ test coverage.
