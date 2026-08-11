# FinanceHub — Master Project Roadmap & Execution Specification

> **Status**: `Approved & Finalized`  
> **Last Updated**: `2026-08-10`  
> **Author**: `FinanceHub Architecture Team & User`

---

## 🎯 Executive Vision & Completion Definition
FinanceHub é um agregador financeiro pessoal estruturado em **Microsserviços .NET 10**. Esta especificação define o roteiro mestre de execução, do scaffolding inicial à prontidão de produção.

---

## 📍 Master Roadmap Blueprint

```text
Phase 1: Shared Modules & Infrastructure Primitives (Concluída a especificação)
   │
Phase 2: AuthConsent Service (OAuth2 / FAPI Manager) (Concluída a especificação)
   │
Phase 3: Bank Integration Services (Itaú & Mercado Pago) (Concluída a especificação - Real Bank Ready)
   │
Phase 4: Transaction Aggregator Service (Canonical Ledger & Deduplication) (Concluída a especificação)
   │
Phase 5: API Gateway / BFF (Aggregated Rest Endpoints) (Concluída a especificação)
   │
Phase 6: Frontend Dashboard (React + Vite + Financial Charts) (Concluída a especificação)
   │
Phase 7: Docker Compose Unificado & Validação E2E (Concluída a especificação)
```

---

## 📋 Especificação Detalhada por Fase (Decisões Aprovadas)

### Phase 1: Módulos Compartilhados & Primitivas de Infraestrutura

- [x] **1.1 Mensageria & Broker Local**:
  - **Decisão**: RabbitMQ via Docker Container (`rabbitmq:3-management`) + MassTransit com **Transactional Outbox Pattern**.
  - **Motivação**: Garantir filas reais, retentativas automáticas, Dead-Letter Queues (DLQ) e fidelidade ao ambiente de produção.
- [x] **1.2 Certificados mTLS (`FinanceHub.Shared.Certificates`)**:
  - **Decisão**: Leitura de arquivo `.pfx` via caminho/User Secrets + **Fallback Mock de Dev**.
  - **Motivação**: Permite rodar o projeto localmente e executar suítes de teste sem depender de um certificado bancário ICP-Brasil real.
- [x] **1.3 Observabilidade & Tracing (`FinanceHub.Shared.Observability`)**:
  - **Decisão**: Jaeger / OpenTelemetry Collector via OTLP gRPC/HTTP + Serilog PII Redaction.
  - **Motivação**: Visualizar graficamente spans de requisições distribuídas entre API Gateway, AuthConsent e Integration Services.

---

### Phase 2: Auth/Consent Service (`FinanceHub.AuthConsent`)

- [x] **2.1 Armazenamento & Renovação de Tokens OAuth2/FAPI**:
  - **Decisão**: Background Worker Service com `PeriodicTimer` / Quartz.NET para renovação proativa antes do tempo de expiração (`expires_in`).
  - **Motivação**: Evitar que chamadas aos serviços de integração bancária falhem por token expirado.

---

### Phase 3: Bank Integration Services (`FinanceHub.ItauIntegration` & `FinanceHub.MercadoPagoIntegration`)

- [x] **3.1 Estratégia de Ingestão de Transações & Teste com Banco Real**:
  - **Decisão**: Sincronização **Sob Demanda / Manual** com suporte a **Credenciais Reais de Banco (Mercado Pago API / Open Finance OAuth2)** + opção de alternar `UseSandbox: false` no `appsettings.Development.json`.
  - **Motivação**: Permitir testar a aplicação com a sua conta bancária real no ambiente local de forma simples e segura via User Secrets.

---

### Phase 4: Transaction Aggregator Service (`FinanceHub.TransactionAggregator`)

- [x] **4.1 Algoritmo de Deduplicação de Transações**:
  - **Decisão**: **Hash SHA-256 Determinístico + ID do Banco (`BankTransactionId`)**.
  - **Mapeamento**: `SHA256(InstituicaoId + ContaId + DataTransacao + Valor + DescricaoOriginal)` com **Índice Único Composto no PostgreSQL**.
  - **Motivação**: Garantia matemática de que compras ou transferências repetidas trazidas por múltiplos extratos sejam gravadas exatamente 1 vez no saldo consolidado.

---

### Phase 5: API Gateway / BFF (`FinanceHub.ApiGateway`)

- [x] **5.1 Autenticação & Estrutura de Endpoints BFF**:
  - **Decisão**: **JWT Bearer Tokens com ASP.NET Core Identity / Login (Email + Senha)**.
  - **Motivação**: Prover controle de acesso seguro para o usuário final no frontend, com geração de `access_token` JWT assinado e suporte a refresh tokens.

---

### Phase 6: Frontend Dashboard (`src/Web/finance-hub-web`)

- [x] **6.1 UI & Visualização de Dados**:
  - **Decisão**: **React + Vite + TailwindCSS + Recharts / Tremor**.
  - **Motivação**: Interface moderna, de alto impacto visual, com suporte nativo a Dark Mode e gráficos responsivos de receitas vs despesas e saldo consolidado.

---

### Phase 7: Orquestração de Ambiente & Execução (`docker-compose.yml`)

- [x] **7.1 Arquivo Docker Compose 100% Unificado**:
  - **Decisão**: **Docker Compose 100% Unificado**.
  - **Motivação**: Subir todos os microsserviços (`AuthConsent`, `ItauIntegration`, `MercadoPagoIntegration`, `TransactionAggregator`, `ApiGateway`), o frontend React, PostgreSQL (DB per service), RabbitMQ e Jaeger em um único comando `docker compose up`.
