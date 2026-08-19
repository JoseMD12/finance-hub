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
