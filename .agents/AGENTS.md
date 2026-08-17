# FinanceHub - .agents Entrypoint & Subagent Specification

This document provides operational instructions and context for subagents, automated skills, and MCP tools operating within the `.agents/` customization layer of **FinanceHub**.

---

## 🎯 Overview & Context

FinanceHub is an enterprise **.NET 10 / C# 13** personal financial aggregation and control platform structured as independent **Microservices** following **Clean Architecture + DDD** per service. It connects directly to Open Finance APIs provided by Brazilian financial institutions (**Itaú**, **Mercado Pago**, and **Banco Inter**).

Subagents operating in this workspace perform autonomous tasks such as scaffolding microservice features, auditing FAPI compliance, executing test suites, and synchronizing project skills.

---

## ⚡ Quick Slash Commands Index

Use these slash commands in chat to instantly trigger specialized harness workflows:

| Slash Command | Target Skill | Primary Purpose |
| :--- | :--- | :--- |
| `/spec-feature` | `spec-feature` | Interactive plan mode specification (1 question at a time). |
| `/scaffold-slice <Service> <UseCase>` | `scaffold-slice` | Scaffold CQRS Command, Query, Handlers (separate `.cs` files) and Endpoints. |
| `/run-tdd` | `run-tdd` | Execute compulsory Red -> Green -> Refactor TDD cycle. |
| `/code-review` | `code-review` | Audit FAPI security, mTLS, LGPD (PII), RFC 7807 exceptions, zero magic strings. |
| `/git-commit` | `git-commit` | Single atomic Conventional Commit of working changes. |
| `/git-commit-many-by <strategy>` | `git-commit-many-by` | Layered (`layer`), Feature (`feature`), or Service (`service`) fractional commits. |
| `/git-pr <destination-branch>` | `git-pr` | Prepare and open Pull Request to target branch with release notes & checklist. |
| `/pr-analyzer [pr-number]` | `pr-analyzer` | Audit GitHub Actions CI, SonarCloud Quality Gate, open issues, and code duplication. |

---

## 🏛️ System Architecture Matrix

```
/
├── .agents/
│   ├── AGENTS.md               <-- Subagent entrypoint & operational rules
│   ├── agents.json             <-- MCP server configurations
│   ├── knowledge/              <-- Domain models & system architecture ADRs
│   ├── rules/                  <-- Modular architectural & security rules
│   └── skills/                 <-- Project skills directory
├── src/
│   ├── Services/               <-- Autonomous Microservices (Clean Arch + DDD)
│   │   ├── ApiGateway/                  <-- BFF Entrypoint
│   │   ├── PluggyIntegration/          <-- Open Finance Connector (Itaú, Inter, MP)
│   │   ├── FileImporter/               <-- Offline Statement & Invoice Parser (OFX, CSV, PDF)
│   │   └── TransactionAggregator/       <-- Canonical Ledger & Deduplication
│   └── Shared/                 <-- Reusable Infrastructure Libraries
│       ├── FinanceHub.Shared.Certificates/ <-- ICP-Brasil mTLS Client Certs
│       ├── FinanceHub.Shared.Messaging/    <-- MassTransit / RabbitMQ + Outbox
│       └── FinanceHub.Shared.Observability/  <-- OpenTelemetry & Logging
└── tests/                      <-- Service Unit & Integration Tests (xUnit)
```

---

## 🔐 Banking & Security Protocol Rules

When generating code or configuring integrations, subagents **must** adhere to:

1. **Microservice Database Isolation**:
   - Each microservice manages its own PostgreSQL database. No cross-service direct DB access is allowed.

2. **Transactional Outbox Pattern**:
   - All asynchronous inter-service domain events are published via Transactional Outbox Pattern using `FinanceHub.Shared.Messaging`.

3. **Financial-Grade API (FAPI 1.0 / 2.0) Profile**:
   - OAuth 2.0 implementation with `private_key_jwt`, Pushed Authorization Requests (PAR), and PKCE.
   - DPoP (Demonstrating Proof-of-Possession) header generation for all protected API calls.

4. **mTLS Handshake Configuration**:
   - All outgoing bank API integrations (Itaú, Banco Inter, Mercado Pago) must rely on `X509Certificate2` client certificates configured via `FinanceHub.Shared.Certificates`.

5. **Data Encryption (AES-256-GCM / KMS)**:
   - Sensitive user tokens, consent tokens, and PII must be encrypted at rest using AES-256-GCM.
   - Keys must be fetched dynamically from KMS (AWS KMS / Azure Key Vault).

6. **LGPD Compliance**:
   - Strictly enforce consent verification before fetching bank statements or balances.
   - Redact all sensitive fields (CPF, account numbers, names) in logs.

7. **DDD Aggregate Root & Rich Domain Model**:
   - Strictly enforce Rich Domain Models (no anemic DTO-like entities with public `get; set;`).
   - Internal entities and Value Objects are encapsulated and managed EXCLUSIVELY through the Aggregate Root (see `.agents/rules/ddd-aggregate-rich-domain.md`).

8. **TDD Mandatory Workflow (Red -> Green -> Refactor)**:
   - All feature implementation MUST start with writing a failing test first (**Red**).
   - Write minimal production code to pass (**Green**), then refactor with domain patterns (**Refactor / Yellow**).
   - Specifications must define test cases upfront before code implementation (see `.agents/rules/tdd-workflow.md`).

9. **Domain Exception Hierarchy & RFC 7807 ProblemDetails**:
   - Domain errors throw strongly-typed exceptions derived from `DomainException` carrying `ErrorCode` and `StatusCode`.
   - APIs handle exceptions globally using native .NET 10 `IExceptionHandler` returning RFC 7807 `ProblemDetails` with `traceId` and `errorCode`. Zero manual `try/catch` in endpoints (see `.agents/rules/exception-handling-rfc7807.md`).

10. **Zero Magic Strings & Zero Magic Numbers**:
    - Never inline magic strings (prefixes, bank identifiers, token action names like `"mp"`, `"access"`, `"refresh"`) or magic numbers.
    - Centralize all constants into strongly-typed domain/infrastructure constants classes (see `.agents/rules/csharp-dotnet10.md`).

11. **Encapsulated Dependency Injection Extension Classes**:
    - Each project layer (`Infrastructure`, `Application`, `Api`) MUST provide an exclusive `DependencyInjection.cs` static extension class (`Add<Layer>Services`).
    - Database `DbContext` registrations and connection strings reside EXCLUSIVELY in the `Infrastructure` DI extension class.

12. **Strict Environment Variable Loading via `.env`**:
    - All environment configurations (database connections, ports, RabbitMQ credentials) MUST be loaded from a `.env` file or environment variables.
    - Zero inline fallback defaults allowed in code. Fail-fast on startup if a required variable is missing.

13. **Mandatory Dependency Inversion for Handlers and Services**:
    - All Command and Query Handlers MUST define and implement an explicit interface (e.g. `ICreateConsentCommandHandler`, `IAuthorizeConsentCommandHandler`, `IRenewTokenCommandHandler`, `IRevokeConsentCommandHandler`, `IGetConsentByUserIdQueryHandler`).
    - API endpoints MUST depend exclusively on these interfaces instead of concrete classes. Only static extension/utility classes are exempt from interfaces.
    - **MANDATORY SEPARATE FILES**: The interface (`I<Name>.cs`) and its implementation (`<Name>.cs`) MUST ALWAYS reside in separate, dedicated `.cs` files. It is strictly forbidden to declare a `public interface` and its implementing `public class` in the same file. Reference pattern: `FinanceHub.AuthConsent.Application` (e.g., `IAuthorizeConsentCommandHandler.cs` + `AuthorizeConsentCommandHandler.cs` as distinct files in the same folder).





---

## 🛠️ Developer & AI Conventions

### Commit Messages
All automated or agent-generated commits must adhere to Conventional Commits:
`<type>(<scope>): <summary>`
- Scopes: `auth`, `itau`, `mercadopago`, `inter`, `aggregator`, `gateway`, `shared`, `harness`.

### Testing Standard
- Frameworks: xUnit, FluentAssertions, NSubstitute.
- Code Coverage: **80% minimum coverage** required per microservice.

---

## 🔄 MCP & Skill Management

- **Skill Location**: `.agents/skills/<skill-name>/SKILL.md`
- **MCP Config File**: `.agents/agents.json`
- **Sync Command**: `agents sync`
- **Validation Command**: `agents sync --check`
- **MCP Test Command**: `agents mcp test --runtime`

When modifying or creating new skills or MCP tool integrations, always run `agents sync --check` to ensure repository integrity.
