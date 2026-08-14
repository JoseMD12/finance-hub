# FinanceHub — Master Project Roadmap & Execution Specification

> **Status**: `In Active Execution`  
> **Last Updated**: `2026-08-13`  
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
Phase 4: Transaction Aggregator Service (Canonical Ledger & Deduplication) (Concluída - 100%)
   │
Phase 5: API Gateway / BFF (Aggregated Rest Endpoints & Resilience) (Concluída - 100%)
   │
Phase 6: Frontend Dashboard (React + Vite + Financial Charts) (Especificado - Próximo Passo)
   │
Phase 7: Docker Compose Unificado & Validação E2E (Especificado)
   │
Phase 8: Módulo IRPF & Tax Analytics (Relatórios & Snapshots de Imposto de Renda) (Stand-By / Planejado)
```

---

## 📋 Especificação Detalhada por Fase & Regras de Arquitetura Incorporadas

### 🛡️ Regras Globais & Convenções do Harness (`.agents/`)
- [x] **Regra 11 — Módulos de DI Exclusivos por Camada**: `DependencyInjection.cs` estritamente separado.
- [x] **Regra 12 — Carregamento Estrito via `.env` (Zero Hardcoded/Magic Defaults)**.
- [x] **Regra 13 — Inversão de Dependência Obrigatória em Handlers & Separate Files**: Interfaces e implementações em arquivos `.cs` estritamente separados.
- [x] **Zero Magic Strings & Hierarquia de Exceções RFC 7807**.

---

### Phase 1: Módulos Compartilhados & Primitivas de Infraestrutura (`Concluída`)
- [x] **1.1 Mensageria & Outbox Pattern (`FinanceHub.Shared.Messaging`)**: MassTransit + RabbitMQ Outbox.
- [x] **1.2 Certificados mTLS (`FinanceHub.Shared.Certificates`)**: ICP-Brasil X509.
- [x] **1.3 Observabilidade (`FinanceHub.Shared.Observability`)**: OpenTelemetry + Serilog + PII Redaction.

---

### Phase 2: Auth/Consent Service (`FinanceHub.AuthConsent`) (`Concluída`)
- [x] **2.1 Aggregate Root `BankConsent` & `ConsentToken`**.
- [x] **2.2 Worker Proativo `TokenRenewalBackgroundService`**.
- [x] **2.3 Endpoints REST & Cobertura de Testes Unitários/Testcontainers**.

---

### Phase 3: Bank Integration Services (`Itaú`, `Mercado Pago` & `Banco Inter`) (`Concluída`)
- [x] **3.1 Connector Itaú, Mercado Pago & Banco Inter (`FinanceHub.InterIntegration`)**.

---

### Phase 4: Transaction Aggregator Service (`FinanceHub.TransactionAggregator`) (`Concluída`)
- [x] **4.1 Ingestão & Ledger Canônico (`CanonicalTransaction`)**.
- [x] **4.2 Deduplicação SHA-256 Idempotente**.
- [x] **4.3 Motor Híbrido de Categorização (`CategoryResolverPipeline`)**.
- [x] **4.4 Saldo Materializado (`account_balances`) com Concorrência Otimista (`xmin`)**.
- [x] **4.5 Publicação Outbox OCP (`TransactionNormalized`, `BankTransactionNormalized`)**.

---

### Phase 5: API Gateway / BFF (`FinanceHub.ApiGateway`) (`Concluída`)

- [x] **5.1 Proxy HTTP & Agregação BFF via Typed Clients**: `IAuthConsentServiceClient` e `ITransactionAggregatorServiceClient` unificando chamadas sem dependência de proxies externos.
- [x] **5.2 Endpoint de Dashboard Agregado**: `GET /api/v1/gateway/dashboard` combinando saldo consolidado e consentimentos em paralelo via `Task.WhenAll`.
- [x] **5.3 Autenticação & Autorização JWT Bearer**: Validação de token com escopos `openfinance:read`, `openfinance:write`, `openfinance:admin`.
- [x] **5.4 Rate Limiting por IP e por User**: Políticas nativas no .NET 10 (30 req/min anônimo, 120 req/min autenticado).
- [x] **5.5 Resiliência & Health Checks Agregados**: Timeout de 10s, políticas Polly e endpoint `/health/detailed`.
- [x] **5.6 Suíte de Testes Unitários do Gateway**: Clientes HTTP e `GlobalExceptionHandler` testados.

---

### Phase 6: Frontend Dashboard (`src/Web/finance-hub-web`) (`Próximo Passo`)
- [ ] React + Vite + TailwindCSS + Recharts para saldo consolidado, extrato e conciliação.
