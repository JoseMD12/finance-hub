---
name: code-review
description: Automated code review skill focusing on financial security, token leakage detection, concurrency bugs, EF Core performance traps, and architectural compliance.
---

# FinanceHub Code Review Skill (.NET 10 / C# 13)

Use this skill when auditing, reviewing, or analyzing C# code, pull requests, or diffs in the **FinanceHub** repository. This skill provides an automated multi-layer code review framework specifically designed for high-precision financial microservices.

---

## 1. Multi-Layer Review Checklist

### Layer 1: Financial Security & Money Precision 💰
- **Type Safety**: MUST use `decimal` for all monetary amounts, interest rates, balances, and fees. NEVER use `float` or `double`.
- **Rounding Rules**: Require explicit rounding mode (`Math.Round(val, 2, MidpointRounding.ToEven)`) on financial operations. Never rely on implicit rounding.
- **Negative Value Handling**: Validate monetary inputs against unauthorized negative values (prevent negative transfers/deposits).
- **Idempotency & Deduplication**: Ensure state-changing financial API endpoints accept and validate an `Idempotency-Key` or compute SHA256 `HashUnico` for transaction deduplication in Redis/DB.

### Layer 2: Token & Secret Leakage Detection 🔒
- **Hardcoded Secrets**: Scan for secrets, private keys, certificates, passwords, API keys (Itaú, Mercado Pago, Inter, Open Finance), or DB connection strings.
- **Log Sanitization**: Ensure sensitive data (PAN, CVV, bank account numbers, passwords, JWT bearer tokens) are NEVER passed into `ILogger` templates or structured parameters.
- **Header Integrity**: Check that Authorization headers and mTLS client certificates (`FinanceHub.Shared.Certificates`) are handled securely without disabling certificate validation in HTTP clients.

### Layer 3: Concurrency & Synchronization Bugs ⚡
- **Optimistic Concurrency**: Financial domain entities modifying balances MUST use EF Core concurrency tokens (`[Timestamp]` or `uint RowVersion`). Catch and handle `DbUpdateConcurrencyException`.
- **Async Hygiene**: Verify proper `CancellationToken` propagation across call chains. Avoid `async void`, `.Result`, or `.Wait()` which cause thread starvation or deadlocks.
- **Outbox Pattern Safety**: Asynchronous events published inside transactional operations MUST use MassTransit / RabbitMQ Transactional Outbox Pattern (`FinanceHub.Shared.Messaging`) to prevent phantom messages or dual-write inconsistencies.

### Layer 4: EF Core Performance Traps 🐢
- **N+1 Query Traps**: Audit queries loading navigation properties inside `foreach` loops. Ensure explicit `.Include()`, `.ThenInclude()`, or DTO projection with `.Select()`.
- **Read-Only Optimization**: Enforce `.AsNoTracking()` on all read-only queries.
- **Client vs Server Evaluation**: Ensure `IQueryable` filtering (`Where`, `Select`) is not evaluated client-side after `.ToList()` or `.ToListAsync()`.
- **Pagination Enforcement**: Large record listings (transactions, ledger entries) MUST enforce `.Take()` and `.Skip()` limits.
- **Bulk Updates**: Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` for mass updates instead of loading entities into memory and calling `SaveChangesAsync()` in a loop.

### Layer 5: Architectural & .NET 10 Compliance 🏗️
- **Clean Architecture & Database-per-Service Boundaries**: 
  - Microservice Domain layer must have ZERO dependencies on Infrastructure, EF Core, ASP.NET Core, or third-party SDKs.
  - Microservices NEVER access another microservice's PostgreSQL database directly.
- **Result Pattern**: Prefer domain `Result<T>` or explicit error models over throwing exceptions for anticipated business logic failures.
- **C# 13 & .NET 10 Features**: Validate proper use of modern features (primary constructors, pattern matching, `field` keyword if applicable, collection expressions).

---

## 2. Review Findings Output Format

When generating code review feedback, structure findings systematically:

### Summary Matrix
- 🔴 **CRITICAL**: Security risks, money calculation bugs, token leakage, cross-database access, data corruption potential.
- 🟠 **HIGH**: Concurrency bugs, missing Transactional Outbox on domain events, severe EF Core performance traps (N+1).
- 🟡 **MEDIUM**: Missing `AsNoTracking()`, unhandled exceptions, missing logging sanitization.
- 🔵 **LOW / NIT**: Code style, naming, minor refactoring opportunities.

### Detailed Finding Template

```markdown
### [SEVERITY] Finding Title

- **Location**: [Filename.cs](file:///path/to/Filename.cs#L45-L52)
- **Category**: [Financial Security / EF Core Performance / Concurrency / Token Leakage / Architecture]
- **Problem**: Explanation of why the code breaks FinanceHub guidelines or risks system failure.
- **Recommended Fix**:

```diff
- Decimal balance = amount * 1.05f;
+ decimal balance = Math.Round(amount * 1.05m, 2, MidpointRounding.ToEven);
```
```

---

## 3. Step-by-Step Code Review Workflow

1. **Fetch Changes**: Run `git diff` or inspect target files/PR branches.
2. **Execute Checklist**: Evaluate code against all 5 Review Checklist layers.
3. **Classify Findings**: Categorize findings by severity (CRITICAL, HIGH, MEDIUM, LOW).
4. **Generate Report**: Output the structured review markdown report with precise line links and recommended fixes.

