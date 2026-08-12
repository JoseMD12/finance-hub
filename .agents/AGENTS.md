# FinanceHub - .agents Entrypoint & Subagent Specification

This document provides operational instructions and context for subagents, automated skills, and MCP tools operating within the `.agents/` customization layer of **FinanceHub**.

---

## 🎯 Overview & Context

FinanceHub is an enterprise **.NET 10 / C# 13** personal financial aggregation and control platform structured as independent **Microservices** following **Clean Architecture + DDD** per service. It connects directly to Open Finance APIs provided by Brazilian financial institutions (**Itaú**, **Mercado Pago**, and **Banco Inter**).

Subagents operating in this workspace perform autonomous tasks such as scaffolding microservice features, auditing FAPI compliance, executing test suites, and synchronizing project skills.

---

## 🏛️ System Architecture Matrix

```
/mnt/c/Code/FastFinance/
├── .agents/
│   ├── AGENTS.md               <-- Subagent entrypoint & operational rules
│   ├── agents.json             <-- MCP server configurations
│   ├── rules/                  <-- Modular architectural & security rules
│   └── skills/                 <-- Project skills directory
├── src/
│   ├── Services/               <-- Autonomous Microservices (Clean Arch + DDD)
│   │   ├── ApiGateway/                  <-- BFF Entrypoint
│   │   ├── AuthConsent/                 <-- FAPI / OAuth2 Consent Manager
│   │   ├── ItauIntegration/             <-- Itaú Open Finance Connector
│   │   ├── MercadoPagoIntegration/      <-- Mercado Pago Connector
│   │   ├── InterIntegration/            <-- Banco Inter Connector (Phase 2)
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
