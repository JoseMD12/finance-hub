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
| `/scaffold-frontend-feature <Feature>` | `scaffold-frontend-feature` | Scaffold React + Vite Feature Slice (api, hooks, components, types, pages). |
| `/run-tdd` | `run-tdd` | Execute compulsory Red -> Green -> Refactor TDD cycle. |
| `/judge` | `code-judge` | Multi-judge tribunal auditing architecture/DDD, QA/security/TDD, and DevOps/CI. |
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
│   ├── Shared/                 <-- Reusable Infrastructure Libraries
│   │   ├── FinanceHub.Shared.Messaging/    <-- MassTransit / RabbitMQ + Outbox
│   │   └── FinanceHub.Shared.Observability/  <-- OpenTelemetry & Logging
│   └── Web/                    <-- Frontend Web Application (Phase 6)
│       └── FinanceHub.Web/             <-- React 19 + Vite + TailwindCSS + TanStack Query
└── tests/                      <-- Service Unit & Integration Tests (xUnit)
```

---

## 🔐 Banking, Security & Frontend Architectural Protocol Rules

When generating code or configuring integrations, subagents **must** adhere to:

1. **Microservice Database Isolation**:
   - Each microservice manages its own PostgreSQL database. No cross-service direct DB access is allowed.

2. **Transactional Outbox Pattern**:
   - All asynchronous inter-service domain events are published via Transactional Outbox Pattern using `FinanceHub.Shared.Messaging`.

3. **Financial-Grade API (FAPI 1.0 / 2.0) Profile**:
   - OAuth 2.0 implementation with `private_key_jwt`, Pushed Authorization Requests (PAR), and PKCE.
   - DPoP (Demonstrating Proof-of-Possession) header generation for all protected API calls.

4. **mTLS & Authentication**:
   - Direct bank API connections use Bearer Token authentication via Meu.Pluggy (OAuth2 HTTPS). `FinanceHub.Shared.Certificates` was decommissioned — mTLS negotiation is handled by the Pluggy platform layer.

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
    - All Command and Query Handlers MUST define and implement an explicit interface.
    - API endpoints MUST depend exclusively on these interfaces instead of concrete classes.
    - **MANDATORY SEPARATE FILES**: The interface (`I<Name>.cs`) and its implementation (`<Name>.cs`) MUST ALWAYS reside in separate, dedicated `.cs` files.

14. **Frontend Feature-Driven Vertical Slices**:
    - React code is structured strictly into autonomous domain slices under `src/features/<feature>/` (`api/`, `components/`, `hooks/`, `types/`, `pages/`).
    - Cross-feature direct imports are strictly forbidden; reusable elements belong in `src/shared/` (see `.agents/rules/react-frontend-architecture.md`).

15. **TanStack Query Caching & Query Keys Factory**:
    - Server State is managed exclusively via TanStack Query v5 with strongly-typed Query Key Factories (`<feature>Keys.ts`). Zero API state duplication in local state (see `.agents/rules/react-query-and-state.md`).

16. **Frontend RFC 7807 Exception Parsing & Toast Feedback**:
    - Axios response interceptors parse `application/problem+json` into `ApiError` instances. User notifications are handled via `Sonner` toasts and `showApiError` (see `.agents/rules/frontend-http-and-rfc7807.md`).

17. **Strict Design Tokens, Styling Centralization, WAI-ARIA & BRL Financial Formatting**:
    - All colors, typography, transition timings, shadows (`shadow-card`, `shadow-elevated`, `shadow-brand`), border radiuses, and hover animations MUST be strictly centralized in `src/index.css` (Tailwind `@theme` tokens) or shared component abstractions.
    - Inline hex colors or arbitrary inline hover states are strictly forbidden.
    - Reusable components use `cn(...)`. Accessible labels (`aria-label`) and Brazilian currency formatting (`formatCurrencyBRL`) are compulsory (see `.agents/rules/frontend-design-system-a11y.md`).

18. **Strict Zero-Emoji Policy & Outline Icons**:
    - Emojis are strictly prohibited anywhere in the interface (toasts, buttons, headers, tables). Use clean typography or vector outline icons (`lucide-react` / SVG).

19. **Componentized Form Controls (Custom Select & Dropdowns)**:
    - Never use unstyled native `<select>`. Form controls and dropdowns must be componentized (`shared/components/Select/`), sharing triggers, elevated floating option menus, keyboard navigation, and theme tokens.

20. **Prohibition of Pure White (`#FFFFFF`) & Off-White Standard**:
    - Pure white (`#FFFFFF`) is strictly forbidden on cards, modals, backgrounds, and form inputs. All surfaces must use the off-white token (`#FAFCFB` / `bg-surface-card`).

21. **Prohibition of Vertical Title Accents**:
    - Never place vertical bar pseudo-elements (`::before` vertical bars, `|`) before section titles or headers. Titles must use clean typography, weight, and spacing.

22. **Prohibition of '&' Character in Titles & Menus**:
    - Never use the '&' character in section titles, headers, modals, or menu items. Always use direct, clean names (e.g. 'Conexões' instead of 'Conexões & Ingestão').

23. **Mandatory Centralized Endpoint Registration**:
    - Backend (.NET 10): All Minimal API endpoints MUST be strictly centralized in dedicated endpoint extension classes (`<Domain>Endpoints.cs` / `Map<Domain>Endpoints()`). Inlining route definitions directly in `Program.cs` is strictly forbidden.
    - Frontend (React): All HTTP API requests and endpoints MUST be strictly centralized in dedicated feature API files (`src/features/<feature>/api/<feature>Api.ts`) or central API constants (`src/shared/api/apiEndpoints.ts`). Hardcoding raw URL endpoint strings inside React components or custom hooks is strictly forbidden.

---


## 🛠️ Developer & AI Conventions

### Commit Messages
All automated or agent-generated commits must adhere to Conventional Commits:
`<type>(<scope>): <summary>`
- Scopes: `auth`, `itau`, `mercadopago`, `inter`, `aggregator`, `gateway`, `web`, `shared`, `harness`.

### Testing Standard
- Backend: xUnit, FluentAssertions, NSubstitute (80% minimum coverage).
- Frontend: Vitest, React Testing Library, MSW.

---

## 🔄 MCP & Skill Management

- **Skill Location**: `.agents/skills/<skill-name>/SKILL.md`
- **MCP Config File**: `.agents/agents.json`
- **Sync Command**: `agents sync`
- **Validation Command**: `agents sync --check`
- **MCP Test Command**: `agents mcp test --runtime`

When modifying or creating new skills or MCP tool integrations, always run `agents sync --check` to ensure repository integrity.

