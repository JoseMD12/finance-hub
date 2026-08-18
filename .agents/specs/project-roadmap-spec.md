# FinanceHub — Master Project Roadmap & Execution Specification

> **Status**: `In Active Execution`  
> **Last Updated**: `2026-08-17`  
> **Author**: `FinanceHub Architecture Team & User`

---

## 🎯 Executive Vision & Completion Definition
FinanceHub é um agregador financeiro pessoal de nível empresarial estruturado em **Microsserviços .NET 10 / C# 13** seguindo **Clean Architecture + Vertical Slice + DDD** por serviço. Conecta-se às APIs do Open Finance Brasil (**Itaú**, **Mercado Pago** e **Banco Inter**) via **Pluggy Integration Engine** e importa extratos bancários offline (`OFX`, `CSV`, `PDF`).

---

## 📍 Master Roadmap Blueprint

```text
Phase 1: Shared Modules & Infrastructure Primitives (Messaging & Observability) (Concluída - 100%)
   │
Phase 2: Pluggy Integration Service (Open Finance Connector for Itaú, Inter & Mercado Pago) (Concluída - 100%)
   │
Phase 3: Transaction Aggregator Service (Canonical Ledger & Deduplication SHA-256) (Concluída - 100%)
   │
Phase 4: Offline File Importer Service (OFX, CSV & PDF Statements Parser Engine) (Em Andamento / Planejado)
   │
Phase 5: API Gateway / BFF (Aggregated REST Endpoints, Pluggy & Aggregator Routing) (Concluída - 100%)
   │
Phase 6: Frontend Web Application (FinanceHub.Web - React 19 + Vite + TailwindCSS) (Em Execução - Próximo Passo)
   │
Phase 7: Docker Compose Unificado & Validação E2E (Especificado)
   │
Phase 8: Módulo IRPF & Tax Analytics (Relatórios & Snapshots de Imposto de Renda) (Stand-By / Planejado)
```

---

## 📋 Especificação Detalhada por Fase & Regras de Arquitetura Incorporadas

### 🛡️ Regras Globais & Convenções do Harness (`.agents/`)
- [x] **Regra 11 — Módulos de DI Exclusivos por Camada**: `DependencyInjection.cs` estritamente separado por camada.
- [x] **Regra 12 — Carregamento Estrito via `.env` (Zero Hardcoded/Magic Defaults)**.
- [x] **Regra 13 — Inversão de Dependência Obrigatória em Handlers & Separate Files**: Interfaces e implementações em arquivos `.cs` estritamente separados.
- [x] **Regra 14-19 — Frontend Architecture Protocols**: Vertical slices, TanStack Query key factories, Sonner RFC 7807 toasts, design tokens Tailwind e zero emojis em `FinanceHub.Web`.
- [x] **Zero Magic Strings & Hierarquia de Exceções RFC 7807**.

---

### Phase 1: Módulos Compartilhados & Primitivas de Infraestrutura (`Concluída`)
- [x] **1.1 Mensageria & Outbox Pattern (`FinanceHub.Shared.Messaging`)**: MassTransit + RabbitMQ Outbox com eventos `TransactionIngested` e `InvoiceItemIngested`.
- [x] **1.2 Observabilidade (`FinanceHub.Shared.Observability`)**: OpenTelemetry + Serilog + PII Redaction.
- [x] **1.3 Decommission de `Shared.Certificates`**: mTLS e autenticação unificados pela plataforma Meu.Pluggy.

---

### Phase 2: Pluggy Integration Service (`FinanceHub.PluggyIntegration`) (`Concluída`)
- [x] **2.1 Conector Único Open Finance para Itaú, Banco Inter e Mercado Pago**.
- [x] **2.2 Ingestão de Contas de Cartão de Crédito e Extratos**.
- [x] **2.3 Polly Resilience Pipeline (Exponential Backoff + Jitter em HTTP 429/5xx)**.
- [x] **2.4 Endpoints REST & Cobertura de Testes Unitários/Integration**.

---

### Phase 3: Transaction Aggregator Service (`FinanceHub.TransactionAggregator`) (`Concluída`)
- [x] **3.1 Ingestão & Ledger Canônico (`CanonicalTransaction`)**.
- [x] **3.2 Deduplicação SHA-256 Idempotente**.
- [x] **3.3 Motor Híbrido de Categorização (`CategoryResolverPipeline`)**.
- [x] **3.4 Saldo Materializado (`account_balances`) com Concorrência Otimista (`xmin`)**.
- [x] **3.5 Publicação Outbox OCP (`TransactionNormalized`, `BankTransactionNormalized`)**.

---

### Phase 4: Offline File Importer Service (`FinanceHub.FileImporter`) (`Em Andamento`)
- [ ] **4.1 Parsers de Arquivos de Extrato (`OFX`, `CSV`, `PDF`)**.
- [ ] **4.2 Normalização de Transações Offline e Envio ao Aggregator**.

---

### Phase 5: API Gateway / BFF (`FinanceHub.ApiGateway`) (`Concluída`)
- [x] **5.1 Proxy HTTP & Agregação BFF via Typed Clients**: `IPluggyIntegrationServiceClient` e `ITransactionAggregatorServiceClient` unificando chamadas sem dependência de proxies externos.
- [x] **5.2 Endpoint de Dashboard Agregado**: `GET /api/v1/gateway/dashboard` combinando saldo consolidado e extratos em paralelo via `Task.WhenAll`.
- [x] **5.3 Autenticação & Autorização JWT Bearer**: Validação de token com escopos `openfinance:read`, `openfinance:write`, `openfinance:admin`.
- [x] **5.4 Rate Limiting por IP e por User**: Políticas nativas no .NET 10 (30 req/min anônimo, 120 req/min autenticado).
- [x] **5.5 Resiliência & Health Checks Agregados**: Timeout de 10s, políticas Polly e endpoint `/health/detailed`.
- [x] **5.6 Suíte de Testes Unitários do Gateway**: Clientes HTTP e `GlobalExceptionHandler` testados.

---

### Phase 6: Frontend Dashboard (`src/Web/FinanceHub.Web`) (`Em Execução`)
- [x] Especificação detalhada em [`.agents/specs/phase-6-frontend-web-spec.md`](./phase-6-frontend-web-spec.md).
- [x] Scaffolding React 19 + Vite + TypeScript + TailwindCSS + TanStack Query + Recharts + Sonner.
- [x] Correção de 100% das 15 issues do SonarCloud no PR #11 e renomeação da pasta para `FinanceHub.Web`.
- [ ] Integração E2E das páginas do frontend (`Dashboard`, `Transações`, `Conexões`, `Login`) com `FinanceHub.ApiGateway`.


