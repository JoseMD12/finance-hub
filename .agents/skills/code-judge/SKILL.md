---
name: code-judge
description: Multi-judge agent evaluation tribunal incorporating automated code review, architectural & DDD audit, financial security & precision, QA & TDD coverage, and DevOps CI/CD pipeline verification.
---

# ⚖️ Code Judge & Multi-Agent Review Tribunal

## ⚡ Trigger / Slash Commands
```bash
/judge                      # Runs the full multi-judge tribunal (Architect, QA/Security, DevOps)
/code-judge                 # Alias for /judge
/judge ddd                  # Runs specifically the Software Architect & DDD Judge
/judge qa                   # Runs specifically the QA, SRE & Security Judge
/judge ci                   # Runs specifically the DevOps & CI/CD Pipeline Judge
/code-review                # Legacy alias triggering the security & code audit judge
```

---

## 🎯 Purpose & Workflow Context

This skill serves as **Gate 4 (Judge)** in the standard FinanceHub feature delivery lifecycle:

```text
1. /plan ──> 2. /spec-feature ──> 3. /run-tdd ──> 4. /judge ──> 5. /git-commit or /git-commit-many-by ──> 6. /git-pr develop ──> 7. /pr-analyzer
```

It launches a tribunal of autonomous, specialized judge subagents that evaluate working changes against the technical specification (`.agents/specs/<spec>.md`), architecture rules (`.agents/rules/`), financial security, EF Core performance, and CI/CD standards.

---

## 🏛️ The Three Specialized Judge Profiles

### 1. 🏛️ Architect & DDD Judge (`architect_ddd_judge`)
- **Clean Architecture & Microservices Boundaries**:
  - Domain layer has ZERO framework dependencies (no EF Core, ASP.NET, or SDKs).
  - Microservices never access another service's PostgreSQL database directly.
  - Mandatory separate files: interface (`I<Name>.cs`) and implementation (`<Name>.cs`).
- **Rich Domain Model & Invariants**:
  - Aggregate roots encapsulate private setters, invariants, and factory methods.
  - Value Objects (`Money`, `TransactionHash`, `AccountIdentifier`, `SanitizedDescription`) are immutable with explicit validation.
- **CQRS & Query Optimization**:
  - Read queries use `.AsNoTracking().Select(...)` direct DTO projections.
  - Ingestion and write commands eliminate redundant database roundtrips and check EF Core change tracker states (`EntityState.Detached`).
- **Spec Alignment**:
  - Validates that code strictly fulfills the approved contracts in `.agents/specs/<feature>-spec.md`.

---

### 2. 🛡️ QA, SRE & Financial Security Judge (`qa_sre_security_judge`)
- **Financial Security & Money Precision**:
  - Mandatory `decimal` for all monetary amounts, interest rates, and fees. NEVER use `float` or `double`.
  - Explicit rounding mode (`Math.Round(val, 2, MidpointRounding.ToEven)` / `AwayFromZero`).
  - Idempotency & deduplication: SHA-256 hash checks and `Idempotency-Key` headers.
- **Token & Secret Leakage Detection**:
  - Zero hardcoded secrets, private keys, certificates, passwords, API tokens, or connection strings.
  - Log sanitization: sensitive data (PAN, CVV, bank account numbers, passwords, JWT tokens) are never logged (LGPD).
- **Concurrency & Outbox Safety**:
  - Concurrency tokens (`[Timestamp]` or PostgreSQL `xmin` rowversion) on balance-modifying entities.
  - Asynchronous inter-service events must use MassTransit Transactional Outbox Pattern (`FinanceHub.Shared.Messaging`).
- **Test Quality & TDD Application**:
  - Comprehensive unit and integration tests using xUnit, FluentAssertions, and Testcontainers.
  - Regression risks and mock alignment (NSubstitute).
- **Error Handling & RFC 7807**:
  - Domain exceptions derive from `DomainException` carrying `ErrorCode` and `StatusCode`.
  - APIs handle exceptions globally using `IExceptionHandler` returning RFC 7807 `ProblemDetails`.

---

### 3. 🚀 DevOps & CI/CD Pipeline Judge (`devops_ci_judge`)
- **GitHub Actions Workflow Orchestration**:
  - Trigger scoping (`main`, `develop`, `pull_request`), concurrency cancellation (`concurrency.cancel-in-progress: true`), and least privilege permissions (`permissions: contents: read`).
  - Parallelization of backend (`.NET 10`) and frontend (`React + Vite`) build/test jobs.
- **Runner Optimization & Caching**:
  - NuGet and npm caching configuration.
  - Redundant compilation elimination (`dotnet test --no-build`).
- **Quality Gates & Artifacts**:
  - Test results (`trx`) and code coverage collection (`--collect:"XPlat Code Coverage"`).
  - Docker multi-stage build alignment and unprivileged user execution (`USER app`, `USER nginx`).

---

## 📋 Evaluation Protocol & Execution Steps

When `/judge` or `/code-judge` is triggered:

### Step 1: Gather Branch Context
1. Identify current branch name: `git branch --show-current`.
2. Inspect unstaged and staged changes: `git status --short` and `git diff`.
3. Locate corresponding spec: `.agents/specs/<feature>-spec.md`.

### Step 2: Define & Spawn Judge Subagents
Use `define_subagent` and `invoke_subagent` to launch the requested judges in parallel.

### Step 3: Empirical Validation
Each judge runs necessary empirical checks:
- `dotnet build --configuration Release`
- `dotnet test --configuration Release`
- Frontend typecheck/test if applicable (`npm test`, `npm run build`).

### Step 4: Consolidated Scorecard & Report
Once all judges return their reports, the master agent synthesizes an Executive Scorecard:

```markdown
# ⚖️ Consolidated Tribunal Evaluation Scorecard

| Dimension / Judge | Score | Status | Key Verdict |
| :--- | :---: | :---: | :--- |
| **1. Architecture & DDD** | `X.X/10` | 🟢 / 🟡 / 🔴 | Summary of architectural alignment |
| **2. QA, SRE & Security** | `X.X/10` | 🟢 / 🟡 / 🔴 | Summary of test coverage & security |
| **3. DevOps & CI/CD** | `X.X/10` | 🟢 / 🟡 / 🔴 | Summary of pipeline efficiency |
| **Composite Score** | **X.X / 10** | **APPROVED / BLOCKED** | **Overall Verdict** |

### 🛠️ Required Fixes (if any)
- [ ] Action item 1
- [ ] Action item 2

### 💡 Recommended Improvements
- Action item 3
```

---

## 🚫 Rejection / Blocking Criteria
A branch MUST be marked **BLOCKED** if any of the following are detected:
1. ❌ Any compilation error or unhandled compiler warning in Release mode.
2. ❌ Any failing unit or integration test.
3. ❌ Direct database cross-access between microservices.
4. ❌ Hardcoded API secrets, tokens, or unencrypted PII.
5. ❌ Breaking API contract change not documented in the specification.
