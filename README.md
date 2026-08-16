# 🚀 FinanceHub — Agregador Financeiro Open Finance Brasil (.NET 10)

[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices%20%2B%20DDD-blue)](#-arquitetura-do-sistema)
[![Security](https://img.shields.io/badge/Security-FAPI%201.0%2F2.0%20%7C%20mTLS-red)](#-segurança--conformidade-bancária)
[![AI Harness](https://img.shields.io/badge/AI%20Harness-.agents-green)](#-harness-de-ia-agents)

**FinanceHub** é uma plataforma corporativa de agregação e controle de finanças pessoais conectada diretamente às APIs do **Open Finance Brasil** (Itaú, Mercado Pago e Banco Inter). Construída sobre **.NET 10** e **C# 13**, a aplicação adota uma arquitetura baseada em **Microsserviços Autônomos** com **Clean Architecture + DDD** em cada serviço, banco de dados isolado por serviço (Database-per-Service) e mensageria resiliente com o **Transactional Outbox Pattern**.

---

## 🏛️ Arquitetura do Sistema

O sistema é dividido em microsserviços especializados e bibliotecas compartilhadas reutilizáveis:

```text
                  ┌──────────────────────────────────────────────┐
                  │          API Gateway / BFF (Front Entry)     │
                  │              FinanceHub.ApiGateway           │
                  └──────────────┬─────────────────┬─────────────┘
                                 │ HTTP            │ HTTP
     ┌───────────────────────────▼──┐   ┌──────────▼────────────────────────────┐
     │  Auth/Consent Service        │   │ Transaction Aggregator / Normalizer   │
     │  OAuth2 / FAPI Consent Flow  │   │ Canonical Ledger & Deduplication      │
     └──────────────┬───────────────┘   └──────────▲────────────────────────────┘
                    │ Valid Token                  │ RabbitMQ (MassTransit)
                    │ (Internal API)               │ TransactionIngested Event
     ┌──────────────┴───────────────┐              │
     │ Bank Integration Services    ├──────────────┘
     │ - Itau Integration           │
     │ - Mercado Pago Integration   │
     │ - Inter Integration (Fase 2) │
     └──────────────────────────────┘
```

### 🧩 Serviços e Responsabilidades (`src/Services/`)

1. **`FinanceHub.AuthConsent`**: Gerenciador de consentimento e tokens OAuth2/OIDC + FAPI. Inicia o consentimento, realiza a troca de autorização por tokens (`access_token`, `refresh_token`) e expõe API interna de tokens válidos por instituição.
2. **`FinanceHub.ItauIntegration`**: Conector da API Open Finance do Itaú. Consome extratos/faturas, traduz o payload proprietário e publica o evento de integração `TransactionIngested`.
3. **`FinanceHub.MercadoPagoIntegration`**: Conector isolado da API do Mercado Pago. Traduz pagamentos e movimentações para o evento `TransactionIngested`.
4. **`FinanceHub.InterIntegration`**: Conector do Banco Inter (a ser implementado na Fase 2 do projeto).
5. **`FinanceHub.TransactionAggregator`**: Consumidor de eventos `TransactionIngested`. Normaliza transações para o modelo canônico, deduplica lançamentos via SHA-256 e persiste o histórico consolidado. Emite `TransactionNormalized`.
6. **`FinanceHub.ApiGateway`**: Ponto único de entrada (BFF) para a aplicação frontend. Realiza agregação de dados e autorização do usuário final.

### 📦 Módulos Compartilhados (`src/Shared/`)

- **`FinanceHub.Shared.Certificates`**: Gerenciador de certificados digitais mTLS X.509 (ICP-Brasil) para conexões seguras com os bancos.
- **`FinanceHub.Shared.Messaging`**: Contratos de eventos (`TransactionIngested`, `TransactionNormalized`) e configuração do MassTransit / RabbitMQ com suporte ao Transactional Outbox Pattern.
- **`FinanceHub.Shared.Observability`**: Instrumentação centralizada do OpenTelemetry (`traceparent`), métricas e logs estruturados com Serilog.

---

## 🤖 Harness de IA (`.agents/`)

Este repositório inclui um **Harness de IA** estruturado para garantir desenvolvimento consistente, autônomo e alinhado aos padrões arquiteturais do projeto:

```text
.agents/
├── AGENTS.md                   # Diretrizes operacionais e guia do subagente
├── GEMINI.md                   # Contexto e restrições para agentes Gemini
├── rules/                      # Regras arquiteturais e de segurança
│   ├── csharp-dotnet10.md      # Idiomas C# 13, record types, Minimal APIs
│   ├── openfinance-security.md # FAPI 1.0/2.0, mTLS, criptografia e LGPD
│   ├── clean-arch-vertical-slice.md # Isolamento de microsserviços e DDD
│   ├── postgres-efcore.md      # Modelagem PostgreSQL, decimal(18,2) e Outbox
│   └── git-and-code-review.md  # Conventional Commits e PR Checklist
├── skills/                     # Workflows e habilidades automatizadas
│   ├── new-bank-service/        # Passo a passo e framework para novos conectores bancários
│   ├── dotnet-vertical-slice/   # Scaffolding de Use Cases e endpoints
│   ├── postgres-migration/      # EF Core Migrations seguras e zero-downtime
│   ├── git-commit/              # Gerador de commits padronizados
│   ├── git-pr/                  # Criador de Pull Requests automatizado
│   └── code-review/             # Revisor automatizado com foco financeiro
├── knowledge/                  # Base de conhecimento de domínio
│   └── domain-model.md          # Especificação de Agregados e Value Objects
└── specs/                      # Especificações técnicas de APIs
    ├── openfinance-brasil-spec.md # Padrões do Open Finance Brasil (BACEN)
    └── bank-connectors-spec.md  # Especificações de APIs do Itaú, MP e Inter
```

---

## 🔐 Segurança & Conformidade Bancária

- **Database per Service**: Cada microsserviço possui e gerencia seu próprio banco PostgreSQL. Acesso direto ao DB de outro serviço é estritamente proibido.
- **Financial-Grade API (FAPI 1.0/2.0)**: Suporte a `private_key_jwt`, Pushed Authorization Requests (PAR) e PKCE.
- **Handshake mTLS**: Conexões de saída com instituições financeiras autenticadas via certificado ICP-Brasil em `FinanceHub.Shared.Certificates`.
- **Criptografia em Repouso**: Tokens de acesso e dados sensíveis criptografados via **AES-256-GCM** / Data Protection API / KMS.
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
- **Escopos**: `auth`, `itau`, `mercadopago`, `inter`, `aggregator`, `gateway`, `shared`, `harness`.
