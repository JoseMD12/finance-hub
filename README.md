# 🚀 FinanceHub — Agregador Financeiro Open Finance Brasil (.NET 10 & React 19)

[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/React-19.0-61DAFB?logo=react)](https://react.dev/)
[![Language](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices%20%2B%20DDD-blue)](#-arquitetura-do-sistema)
[![Security](https://img.shields.io/badge/Security-FAPI%201.0%2F2.0%20%7C%20mTLS-red)](#-segurança--conformidade-bancária)
[![AI Harness](https://img.shields.io/badge/AI%20Harness-.agents-green)](#-harness-de-ia-agents)

**FinanceHub** é uma plataforma corporativa de agregação e controle de finanças pessoais conectada diretamente às APIs do **Open Finance Brasil** (Itaú, Mercado Pago e Banco Inter). Construída sobre **.NET 10**, **C# 13** e **React 19 (Vite + TailwindCSS)**, a aplicação adota uma arquitetura baseada em **Microsserviços Autônomos** com **Clean Architecture + DDD** em cada serviço, banco de dados isolado por serviço (Database-per-Service), mensageria resiliente com o **Transactional Outbox Pattern** e conciliação bancária em tempo real.

---

## 🏛️ Arquitetura do Sistema

O sistema é dividido em microsserviços especializados, aplicação web SPA e bibliotecas compartilhadas reutilizáveis:

```text
                  ┌──────────────────────────────────────────────┐
                  │             Web Application (React 19)       │
                  │             src/Web/FinanceHub.Web           │
                  └──────────────────────┬───────────────────────┘
                                         │ HTTP / REST
                  ┌──────────────────────▼───────────────────────┐
                  │          API Gateway / BFF (Front Entry)     │
                  │              FinanceHub.ApiGateway           │
                  └──────────────┬─────────────────┬─────────────┘
                                 │ HTTP            │ HTTP
     ┌───────────────────────────▼──┐   ┌──────────▼────────────────────────────┐
     │  Pluggy Integration          │   │ Transaction Aggregator / Normalizer   │
     │  Open Finance Connector      │   │ Canonical Ledger & Deduplication      │
     │  (Itaú, Inter, Mercado Pago) │   │ (Categorization, Rules & Balance)     │
     └──────────────┬───────────────┘   └──────────▲────────────────────────────┘
                    │                              │ RabbitMQ (MassTransit)
                    │                              │ - TransactionIngested
     ┌──────────────┴───────────────┐              │ - InvoiceItemIngested
     │  File Importer               ├──────────────┘ - TransactionsBatchIngested
     │  Offline Parser (.OFX, .CSV) │                - AccountBalanceSnapshotSynchronized
     └──────────────────────────────┘
```

### 🧩 Serviços e Responsabilidades (`src/Services/` & `src/Web/`)

1. **`FinanceHub.Web`**: Aplicação SPA desenvolvida em React 19 + Vite + TailwindCSS + TanStack Query. Apresenta o Livro Razão de Transações, cards de Saldo Real Consolidado vs Liquidez Projetada, gerenciamento de categorias e regras de categorização automática.
2. **`FinanceHub.ApiGateway`**: Ponto único de entrada (BFF) para a aplicação frontend. Realiza agregação de dados do dashboard, autenticação JWT, rate limiting e roteamento.
3. **`FinanceHub.PluggyIntegration`**: Conector unificado de Open Finance Pessoal (via Meu.Pluggy), cobrindo Itaú, Banco Inter e Mercado Pago com publicação assíncrona de eventos contábeis e snapshots de saldo.
4. **`FinanceHub.FileImporter`**: Motor de importação offline para arquivos de extratos e faturas históricas (`.ofx`, `.csv`, `.pdf`).
5. **`FinanceHub.TransactionAggregator`**: Consumidor de eventos contábeis. Normaliza transações para o modelo canônico, deduplica lançamentos via SHA-256, executa o motor de neutralidade de transferências/repasses (volume fantasma), categoriza via regras de usuário/dataset brasileiro e persiste o histórico consolidado.

> **Nota de Histórico Arquitetural**: Os microsserviços legados de conexão direta individual (`AuthConsent`, `ItauIntegration`, `MercadoPagoIntegration`, `InterIntegration`) e a biblioteca `FinanceHub.Shared.Certificates` foram consolidados e substituídos pelo conector unificado `PluggyIntegration` e o motor offline `FileImporter`. Consulte o [ADR de Arquitetura](.agents/knowledge/system-architecture-and-services.md).

### 📦 Módulos Compartilhados (`src/Shared/`)

- **`FinanceHub.Shared.Messaging`**: Contratos de eventos (`TransactionIngested`, `InvoiceItemIngested`, `AccountBalanceSnapshotSynchronized`) e configuração do MassTransit / RabbitMQ com suporte ao Transactional Outbox Pattern.
- **`FinanceHub.Shared.Observability`**: Instrumentação centralizada do OpenTelemetry (`traceparent`), métricas, logs estruturados com Serilog e tratamento global de exceções RFC 7807 (`GlobalExceptionHandler`).

---

## 🔗 Funcionalidades Chave da Tela de Conexões

- **Hub de Conexões Bancárias Open Finance**: Visualização e gerenciamento centralizado de conexões ativas e saldos por instituição bancária (**Itaú**, **Banco Inter** e **Mercado Pago**).
- **Sincronização Online Assíncrona & Polling**: Disparo de sincronização automática com feedback visual de progresso, polling de jobs assíncronos (`202 Accepted`) e renovação de tokens de acesso.
- **Micro-interações e Visual Neon/Glow**: Cards de instituição com efeitos de iluminação em borda (*GlowCard*), métricas de saldo consolidado e estado de saúde da conexão (Badge de sincronização).
- **Importação Offline de Arquivos**: Card dedicado para ingestão manual de extratos e faturas históricas nos formatos `.ofx`, `.csv` e `.pdf` via `FileImporter`.
- **Modais Educativos de Timing Open Finance**: Orientação ao usuário sobre prazos de liquidação e atraso de sincronização bancária (até 48h para faturas e cartões).

---

## 💡 Funcionalidades Chave da Tela de Transações

- **Saldo Real Consolidado vs. Liquidez Projetada**: Cálculo dinâmico do saldo bancário sincronizado vs. liquidez projetada considerando faturas de cartão de crédito abertas.
- **Motor de Neutralidade & Volume Fantasma**: Detecção automática e manual (`IsIgnoredInTotals`) de transferências entre contas próprias, repasses a terceiros e dinheiro de trânsito para evitar duplicidade de receita/despesa nos totais.
- **Marcação de Quitação de Faturas**: Marcação dedicada de pagamentos de fatura de cartão de crédito (`IsBillPayment`) com exibição de badges duplos (`Fatura` + `Neutro`).
- **Categorização Inteligente & Regras do Usuário**: Classificação automática com base no catálogo de estabelecimentos brasileiros e suporte a regras customizadas por usuário com retrocategorização em lote (`ApplyToPastTransactions`).

---

## 🤖 Harness de IA (`.agents/`)

Este repositório inclui um **Harness de IA** estruturado para garantir desenvolvimento consistente, autônomo e alinhado aos padrões arquiteturais do projeto:

```text
.agents/
├── AGENTS.md                   # Diretrizes operacionais, slash commands e regras de protocolo
├── GEMINI.md                   # Contexto e restrições para agentes Gemini
├── rules/                      # Regras arquiteturais, de segurança e frontend
│   ├── csharp-dotnet10.md      # Idiomas C# 13, record types, Minimal APIs
│   ├── openfinance-security.md # FAPI 1.0/2.0, mTLS, criptografia e LGPD
│   ├── clean-arch-vertical-slice.md # Isolamento de microsserviços e DDD
│   ├── react-frontend-architecture.md # Slices verticais sob src/features/<feature>/
│   ├── react-query-and-state.md # Gerenciamento de Server State via TanStack Query
│   ├── exception-handling-rfc7807.md # Trata erros globais RFC 7807 ProblemDetails
│   └── ddd-aggregate-rich-domain.md  # Rich Domain Model e proteção de agregados
├── skills/                     # Workflows e habilidades automatizadas
│   ├── scaffold-slice/         # Scaffolding CQRS .NET 10 (Command, Query, Handler, Endpoint)
│   ├── scaffold-frontend-feature/# Scaffolding de Slices React + Vite
│   ├── run-tdd/                # Workflow de TDD compulsório (Red -> Green -> Refactor)
│   ├── code-judge/             # Tribunal de auditoria técnica (DDD, QA/Security, DevOps)
│   ├── manual-api-curl-testing/# Runner autônomo de testes de integração e endpoints
│   ├── pr-analyzer/            # Analisador de CI, SonarCloud e duplicação em PRs
│   ├── git-commit/             # Gerador de commits padronizados
│   └── git-pr/                 # Criador de Pull Requests automatizado
├── knowledge/                  # Base de conhecimento de domínio
│   ├── domain-model.md          # Especificação de Agregados, Regras de Negócio e Value Objects
│   └── frontend-api-contracts.md# Contratos de API REST entre Gateway e Frontend
└── specs/                      # Especificações técnicas de soluções e relatórios de design
    ├── real-balance-reconciliation-and-view-spec.md # Especificação de saldo real e liquidez
    ├── internal-transfers-and-ghost-volume-deduplication-spec.md # Deduplicação e neutralidade
    └── transactions-and-categories-spec.md # Livro razão e catálogo de categorias
```

---

## 🔐 Segurança & Conformidade Bancária

- **Database per Service**: Cada microsserviço possui e gerencia seu próprio banco PostgreSQL. Acesso direto ao DB de outro serviço é estritamente proibido.
- **Financial-Grade API (FAPI 1.0/2.0)**: Suporte a `private_key_jwt`, Pushed Authorization Requests (PAR) e PKCE quando aplicável.
- **Conexões Seguras**: Conexões com o ecossistema Open Finance executadas via canais seguros HTTPS/OAuth2 gerenciados pelo `PluggyIntegration`.
- **Criptografia em Repouso**: Tokens de acesso e dados sensíveis criptografados via **AES-256-GCM** / Data Protection API / KMS com envelope encryption.
- **Conformidade LGPD**: Sanitização de logs sem exibição de PII (CPF, nomes ou chaves JWT) e rastreabilidade total do ciclo de vida dos consentimentos.

---

## ⚡ Como Executar o Projeto

### Pré-requisitos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ou superior
- PostgreSQL 16+
- RabbitMQ 3.12+ (opcional para execução com mensageria local)

### Compilação da Solução

```bash
dotnet build
```

### Execução dos Testes Unitários

```bash
dotnet test
```

### Executando o API Gateway Localmente

```bash
dotnet run --project src/Services/ApiGateway/FinanceHub.ApiGateway/FinanceHub.ApiGateway.csproj --urls "http://localhost:5050"
```

Acesse o status de saúde do serviço:

```bash
curl http://localhost:5050/health
```

---

## 📝 Convenções de Commit

Todas as contribuições e commits gerados por agentes devem seguir a especificação **Conventional Commits**:

```text
<type>(<scope>): <resumo imperativo>
```

- **Tipos**: `feat`, `fix`, `security`, `arch`, `refactor`, `test`, `docs`, `chore`.
- **Escopos**: `pluggy`, `fileimporter`, `aggregator`, `gateway`, `web`, `shared`, `harness`.
