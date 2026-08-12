# FinanceHub — Master Project Roadmap & Execution Specification

> **Status**: `In Active Execution`  
> **Last Updated**: `2026-08-12`  
> **Author**: `FinanceHub Architecture Team & User`

---

## 🎯 Executive Vision & Completion Definition
FinanceHub é um agregador financeiro pessoal de nível empresarial estruturado em **Microsserviços .NET 10 / C# 13** seguindo **Clean Architecture + Vertical Slice + DDD** por serviço. Conecta-se às APIs do Open Finance Brasil (**Itaú**, **Mercado Pago** e **Banco Inter**).

---

## 📍 Master Roadmap Blueprint

```text
Phase 1: Shared Modules & Infrastructure Primitives (Concluída - 100%)
   │
Phase 2: AuthConsent Service (OAuth2 / FAPI Manager) (Concluída a Implementação, Testes Unitários & E2E HTTP - 100%)
   │
Phase 3: Bank Integration Services (Itaú, Mercado Pago & Banco Inter) (Concluído Scaffolding & Mocks FAPI - 100%)
   │
Phase 4: Transaction Aggregator Service (Canonical Ledger & Deduplication) (Especificado - Em Progresso)
   │
Phase 5: API Gateway / BFF (Aggregated Rest Endpoints) (Especificado)
   │
Phase 6: Frontend Dashboard (React + Vite + Financial Charts) (Especificado)
   │
Phase 7: Docker Compose Unificado & Validação E2E (Especificado)
```

---

## 📋 Especificação Detalhada por Fase & Regras de Arquitetura Incorporadas

### 🛡️ Regras Globais & Convenções do Harness (`.agents/`)
- [x] **Regra 11 — Módulos de DI Exclusivos por Camada**:
  - `DependencyInjection.cs` estritamente separado em `Infrastructure` (`AddAuthConsentInfrastructure`) e `Api` (`AddAuthConsentApi`). Configurações de `DbContext` residem EXCLUSIVAMENTE em `Infrastructure`.
- [x] **Regra 12 — Carregamento Estrito via `.env` (Zero Hardcoded/Magic Defaults)**:
  - Todas as variáveis de ambiente carregadas via `DotNetEnv.Env.TraversePath().Load()`. Suporte local seguro para `InMemory` e `.gitignore` atualizado para ignorar `.env` mantendo `.env.example`.
- [x] **Regra 13 — Inversão de Dependência Obrigatória em Handlers**:
  - Todos os Command Handlers e Query Handlers definem e implementam interfaces explícitas (`ICreateConsentCommandHandler`, `IAuthorizeConsentCommandHandler`, `IRenewTokenCommandHandler`, `IRevokeConsentCommandHandler`, `IGetConsentByUserIdQueryHandler`). Minimal APIs dependem exclusivamente de interfaces.
- [x] **Zero Magic Strings & Utility Variadic API**:
  - Removidos magic strings e interpolações em tokens. `TokenMockGenerator.GenerateToken(params string[] actions)` aceita múltiplos `TokenActions` concatenados limpos.
- [x] **Hierarquia de Exceções de Domínio & RFC 7807**:
  - Exceções fortemente tipadas derivadas de `DomainException` com mensagens padrão amigáveis em Português (`ConsentNotFoundException`, `InvalidOAuthAuthCodeException`, etc.) traduzidas globalmente para RFC 7807 `ProblemDetails`.

---

### Phase 1: Módulos Compartilhados & Primitivas de Infraestrutura (`Concluída`)

- [x] **1.1 Mensageria & Broker Local (`FinanceHub.Shared.Messaging`)**:
  - **Decisão**: RabbitMQ + MassTransit com **Transactional Outbox Pattern**.
  - **Eventos**: `BankAccountLinked` e `TransactionIngested`.
- [x] **1.2 Certificados mTLS (`FinanceHub.Shared.Certificates`)**:
  - **Decisão**: Suporte a certificados `X509Certificate2` ICP-Brasil com fallback mock de dev.
- [x] **1.3 Observabilidade & Tracing (`FinanceHub.Shared.Observability`)**:
  - **Decisão**: OpenTelemetry + Serilog com **PII Redaction (LGPD)** e exportação OTLP/Jaeger.

---

### Phase 2: Auth/Consent Service (`FinanceHub.AuthConsent`) (`Concluída`)

- [x] **2.1 Entidades de Domínio & Aggregate Root**:
  - Aggregate Root `BankConsent` e Owned Entity `ConsentToken` com modelo rico e encapsulado.
- [x] **2.2 Use Cases (CQRS Commands & Queries)**:
  - `CreateConsentCommand` / `CreateConsentCommandHandler`
  - `AuthorizeConsentCommand` / `AuthorizeConsentCommandHandler`
  - `RenewTokenCommand` / `RenewTokenCommandHandler`
  - `RevokeConsentCommand` / `RevokeConsentCommandHandler`
  - `GetConsentByUserIdQuery` / `GetConsentByUserIdQueryHandler`
- [x] **2.3 Worker Proativo de Renovação de Tokens**:
  - Background Service `TokenRenewalBackgroundService` renovando tokens com antecedência configurável.
- [x] **2.4 Suíte de Testes Unitários & Integração**:
  - 38/38 testes passando com xUnit, FluentAssertions e NSubstitute.
- [x] **2.5 Validação Manual HTTP E2E**:
  - Testado e validado em runtime local com `curl` cobrindo fluxo completo: `POST /consents` (Create), `GET /consents/user/{id}`, `POST /authorize`, `POST /refresh`, `DELETE /consents/{id}` (Revoke).

---

### Phase 3: Bank Integration Services (`Itaú`, `Mercado Pago` & `Banco Inter`) (`Concluída`)

- [x] **3.1 Connector Itaú (`FinanceHub.ItauIntegration`)**:
  - OAuth2 FAPI strategy (`ItauOAuthStrategy`) e mapping de extratos bancários.
- [x] **3.2 Connector Mercado Pago (`FinanceHub.MercadoPagoIntegration`)**:
  - API connector Mercado Pago (`MercadoPagoOAuthStrategy`) com ingestão de pagamentos/saldo.
- [x] **3.3 Connector Banco Inter (`FinanceHub.InterIntegration`)**:
  - Estruturação completa do microsserviço `FinanceHub.InterIntegration` (Domain, Application, Infrastructure, Api) com `InterOAuthStrategy`, `InterApiClient` e suíte de testes dedicados (`InterIntegrationTests.cs`). Commit isolado realizado (`feat(inter)`).

---

### Phase 4: Transaction Aggregator Service (`FinanceHub.TransactionAggregator`) (`Próximo Passo`)

- [ ] **4.1 Ingestão & Ledger Canônico**:
  - Consumo de eventos `TransactionIngested` publicados via Outbox pelos conectores bancários.
- [ ] **4.2 Deduplicação Determinística**:
  - Hash SHA-256 (`SHA256(InstituicaoId + ContaId + DataTransacao + Valor + DescricaoOriginal)`) com índice composto no PostgreSQL.
- [ ] **4.3 Consultas & Consolidação de Saldo**:
  - Endpoints CQRS para saldo consolidado, histórico de extratos categorizados e conciliação.

---

### Phase 5: API Gateway / BFF (`FinanceHub.ApiGateway`) (`Pendente`)

- [ ] **5.1 Entrypoint Rest & Autenticação JWT**:
  - Proxy reverso YARP / Ocelot + autenticação JWT Bearer para a aplicação web.

---

### Phase 6: Frontend Dashboard (`src/Web/finance-hub-web`) (`Pendente`)

- [ ] **6.1 Dashboard Financeiro**:
  - React + Vite + TailwindCSS + Recharts para saldo consolidado, conciliação e navegação de contas conectadas.

---

### Phase 7: Docker Compose & Orquestração Unificada (`Pendente`)

- [ ] **7.1 Arquivo Docker Compose Unificado**:
  - Orquestração completa de todos os 6 microsserviços, PostgreSQL (DB por serviço), RabbitMQ e Jaeger.
