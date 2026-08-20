# Especificação Técnica: Evolução Arquitetural, Governança NetArchTest, Idempotência e Resiliência

**Documento:** `.agents/specs/architectural-evolution-spec.md`  
**Status:** 🟢 `Aprovada para Implementação`  
**Data:** 19/08/2026  
**Escopo:** Todo o ecossistema FinanceHub (.NET 10 Microservices & Shared Modules)

---

## 1. 🎯 Objetivo & Visão Geral

Estabelecer a especificação técnica executável para elevar a maturidade arquitetural, governança de código, resiliência, desempenho e segurança do **FinanceHub**:

1. **Fase 1: Governança Arquitetural com `NetArchTest.eNet` & Reorganização da Suíte de Testes**:
   - Renomear `FinanceHub.UnitTests` para `FinanceHub.Tests` e estruturar em 3 subpastas dedicadas: `Architecture/`, `Unit/` e `Integration/`.
   - Implementar testes automatizados de arquitetura com `NetArchTest.eNet` para proteger Clean Architecture, DDD, Regra 13 e isolamento de banco de dados entre microsserviços.
2. **Fase 2: Consumo Idempotente & Outbox/Inbox Pattern**:
   - Tabela `inbox_processed_messages` (SHA-256) e middleware de consumo idempotente no MassTransit (`IdempotentConsumerFilter`).
3. **Fase 3: Resiliência Polly v8 & Propagação W3C TraceContext**:
   - Pipelines Polly v8 nativos em .NET 10 (`AddResilienceHandler`: Circuit Breaker, Rate Limiting com Jitter e Timeout) e propagação W3C `traceparent`.
4. **Fase 4: Desempenho com CQRS Read Models**:
   - Projeção de leitura materializada `user_consolidated_balance_read_model` para consultas $O(1)$ no Gateway/BFF.
5. **Fase 5: Segurança, LGPD & Envelope Encryption (AES-256-GCM / KMS)**:
   - Criptografia de PII e tokens em repouso via Envelope Encryption e redação automática de propriedades sensíveis no Serilog.

---

## 2. 🏛️ Decisões Arquiteturais Confirmadas

### 2.1 Decisão 1: Reorganização da Suíte de Testes & Governança com NetArchTest (Fase 1)
- **Escolha**: Renomeação do projeto de testes para **`FinanceHub.Tests`** (`tests/FinanceHub.Tests/FinanceHub.Tests.csproj`) com organização em 3 pastas internas e suporte a marcas de filtro (`[Trait("Category", "...")]`).
- **Estrutura de Diretórios Alvo**:
  ```text
  tests/FinanceHub.Tests/
  ├── Architecture/          # Testes de Governança Arquitetural (NetArchTest.eNet)
  │   ├── DomainLayerTests.cs
  │   ├── ApplicationLayerTests.cs
  │   ├── HandlerInterfaceConventionTests.cs
  │   └── MicroservicesDatabaseIsolationTests.cs
  ├── Unit/                  # Testes Unitários (NSubstitute + FluentAssertions)
  │   ├── ApiGateway/
  │   ├── PluggyIntegration/
  │   ├── TransactionAggregator/
  │   └── Shared/
  └── Integration/           # Testes de Integração com Testcontainers (PostgreSQL & RabbitMQ)
      ├── PostgresContainerIntegrationTests.cs
      └── RabbitMqContainerIntegrationTests.cs
  ```
- **Comandos de Execução Filtrados**:
  - Testes Unitários: `dotnet test --filter "Category=Unit"`
  - Testes de Arquitetura: `dotnet test --filter "Category=Architecture"`
  - Testes de Integração: `dotnet test --filter "Category=Integration"`
  - Execução Completa: `dotnet test`

### 2.2 Decisão 2: Consumo Idempotente & Outbox/Inbox Pattern (Fase 2)
- **Escolha**: Middleware Genérico MassTransit (`IdempotentConsumerFilter`) + Tabela `inbox_processed_messages`.
- **Detalhamento**:
  - No `FinanceHub.TransactionAggregator.Infrastructure`, criar tabela `inbox_processed_messages` com chave primária no `message_hash` (SHA-256) e timestamp de processamento.
  - Implementar filtro genérico de consumo MassTransit (`IFilter<ConsumeContext<T>>`) que extrai ou calcula o hash determinístico da transação/lote.
  - Se a mensagem já foi processada anteriormente, o filtro interrompe o pipeline e confirma a mensagem (*Ack*) imediatamente sem re-executar os handlers ou alterar estado de banco.

### 2.3 Decisão 3: Resiliência HTTP Polly v8 & W3C TraceContext (Fase 3)
- **Escolha**: Pipeline Polly v8 Nativo (`AddResilienceHandler` em .NET 10).
- **Detalhamento**:
  - No `FinanceHub.PluggyIntegration.Infrastructure`, registrar cliente HTTP resiliente com `AddResilienceHandler("PluggyPipeline")`:
    1. **Circuit Breaker**: Abre circuito após 5 falhas consecutivas (HTTP 5xx) em uma janela de 30s.
    2. **Rate Limiting / Jitter**: Resposta a HTTP 429 com retentativas calculadas por algoritmo de *exponential backoff + random jitter*.
    3. **Timeout Limite**: Timeout total por requisição de 10 segundos.
  - Propagação do cabeçalho W3C `traceparent` ativada nativamente no OpenTelemetry e HttpClientFactory.

### 2.4 Decisão 4: Projeções de Leitura CQRS & Segurança FAPI/LGPD (Fases 4 e 5)
- **Escolha**: Read Models Materializados + Envelope Encryption (AES-256-GCM com KMS) + Redação Serilog.
- **Detalhamento**:
  - **CQRS Read Models (Fase 4)**: Tabela de projeção `user_consolidated_balance_read_model` atualizada assincronamente ao consumir `BankTransactionNormalized` e `TransactionNormalized`, reduzindo consultas agregadas `SUM()` no Gateway/BFF para leituras diretas em $O(1)$.
  - **Criptografia Envelope (Fase 5)**: Tokens de acesso Open Finance e dados PII (CPF, números de conta) são criptografados com **AES-256-GCM** com chave de dados gerada dinamicamente via KMS (AWS KMS / Azure Key Vault / Mock KMS local).
  - **Redação Automática de Logs**: Serilog destructuring com manipulador customizado substituindo valores de propriedades sensíveis (`Cpf`, `AccountNumber`, `AccessToken`, `PluggyToken`) por `"***REDACTED***"`.

---

## 3. 🧩 Estrutura de Arquivos Target & Contratos

```text
tests/FinanceHub.Tests/
├── Architecture/
│   ├── DomainLayerTests.cs
│   ├── ApplicationLayerTests.cs
│   ├── HandlerInterfaceConventionTests.cs
│   └── MicroservicesDatabaseIsolationTests.cs
├── Unit/
└── Integration/

src/Services/TransactionAggregator/FinanceHub.TransactionAggregator.Infrastructure/
├── Messaging/
│   └── Filters/
│       └── IdempotentConsumerFilter.cs
└── Persistence/
    └── Configurations/
        └── InboxProcessedMessageConfiguration.cs
```

---

## 4. 🧪 Plano de Execução & Checklist de Validação

- [x] **Fase 1: Governança & Renomeação da Suíte de Testes**
  - [x] Renomear `FinanceHub.UnitTests.csproj` para `FinanceHub.Tests.csproj` em `tests/FinanceHub.Tests/`.
  - [x] Organizar diretórios em `Architecture/`, `Unit/` e `Integration/`.
  - [x] Instalar `NetArchTest.Rules` e implementar suíte de testes de regras Clean Arch & DDD.
- [x] **Fase 2: Consumo Idempotente & Outbox/Inbox Pattern**
  - [x] Criar entidade/tabela `inbox_processed_messages`.
  - [x] Implementar `IdempotentConsumerFilter` no MassTransit.
- [x] **Fase 3: Resiliência Polly v8 & W3C Tracing**
  - [x] Registrar pipeline Polly v8 (`AddResilienceHandler`) no `PluggyIntegration`.
  - [x] Garantir propagação de W3C `traceparent` em chamadas HTTP e RabbitMQ.
- [x] **Fase 4: CQRS Read Models**
  - [x] Criar projeção de leitura `UserConsolidatedBalanceReadModel`.
- [x] **Fase 5: Segurança, LGPD & Envelope Encryption**
  - [x] Implementar `EnvelopeEncryptionService` com AES-256-GCM.
  - [x] Configurar redação de PII no Serilog.
