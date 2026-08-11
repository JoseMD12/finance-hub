# Git Standards, Code Review & PR Safety Checklist

This document establishes the version control guidelines, commit conventions, code review standards, and PR safety procedures for **FinanceHub**.

---

## 1. Conventional Commits Specification
All commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) format to enable automated changelogs and semantic versioning.

### Format
```text
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

### Allowed Types
* `feat`: A new feature for the user or system (e.g., `feat(itau): add Pix payment execution strategy`).
* `fix`: A bug fix (e.g., `fix(ledger): fix decimal rounding issue on credit settlement`).
* `refactor`: Code change that neither fixes a bug nor adds a feature.
* `perf`: A code change that improves performance (e.g., `perf(query): add compiled async query for balance check`).
* `sec`: Security-related changes or hardening (e.g., `sec(oauth): mandate FAPI PKCE verification`).
* `test`: Adding missing tests or correcting existing tests.
* `docs`: Documentation updates only.
* `chore`: Maintenance, build tasks, or dependency updates.

### Rules
* Subject line must be lower-case, concise (max 72 chars), and written in imperative mood ("add", not "added" or "adds").
* Breaking changes must include `BREAKING CHANGE:` in the footer or a `!` after the type/scope (e.g., `feat(api)!: modify payment endpoint response schema`).

---

## 2. Financial Application PR Safety Checklist
Before submitting or approving a Pull Request in FinanceHub, the PR author and reviewers MUST check all items:

### Security & Privacy
- [ ] **Zero PII Exposure**: Verified that no log statements contain CPF, CNPJ, account numbers, card CVVs, or raw names.
- [ ] **No Exposed Secrets**: Guaranteed that no private keys, JWT tokens, API secrets, or certificates are committed.
- [ ] **OpenFinance Security**: OAuth 2.0 PKCE, mTLS header validation, and token encryption comply with `openfinance-security.md`.

### Financial Data & Math Integrity
- [ ] **Decimal Precision**: All monetary calculations use `decimal` / `numeric(18,4)`. No `float` or `double` used.
- [ ] **Double-Entry Ledger**: Ledger operations maintain balanced debits and credits ($\sum \text{Debits} = \sum \text{Credits}$).
- [ ] **Idempotency**: All state-mutating endpoints enforce `Idempotency-Key` unique constraints.
- [ ] **Race Conditions**: Multi-step state transitions use row locking (`FOR UPDATE`), optimistic concurrency tokens (`RowVersion`), or atomic database transactions.

### Database & Migrations
- [ ] **Migration Safety**: EF Core migrations are non-blocking (`CREATE INDEX CONCURRENTLY`) and verified with `Down()` rollback testing.
- [ ] **Query Performance**: Read-only queries use `.AsNoTracking()`. Verified no N+1 query patterns exist.

### Quality & Architecture
- [ ] **Vertical Slice**: Connector implementations are isolated in Strategy adapters without domain leaks (`clean-arch-vertical-slice.md`).
- [ ] **Tests**: Unit tests cover core business domain logic; integration tests verify API slice handlers.

---

## 3. Static Analysis & Automated Code Review Standards
FinanceHub enforces a strict zero-warning policy across all projects.

### Mandatory Compiler Settings
All `.csproj` files in the repository must enable strict Roslyn analysis settings:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

### Static Analysis Tools
1. **Roslyn Analyzers**: Built-in C# code style (`IDE*`) and security (`CA*`) rules are enforced on build.
2. **SonarQube / Security Code Scan**: Automated CI pipeline blocks PRs containing Security Hotspots or Bugs of severity `Major` or higher.
3. **Dependency Vulnerability Scanning**: `dotnet list package --vulnerable` runs automatically in CI. Any vulnerability of severity `High` or `Critical` breaks the build.
