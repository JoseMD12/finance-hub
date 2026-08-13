# Spec: OUTBOX-001 — Publicação de `TransactionNormalized` via MassTransit Outbox
**Feature Branch**: `feature/aggregator-outbox`
**Serviço**: `FinanceHub.TransactionAggregator`
**Status**: 🔵 EM ANDAMENTO — Refactoring de hierarquia de eventos

---

## 1. Objetivo

Após persistir uma `CanonicalTransaction` com sucesso no banco de dados, o `IngestTransactionCommandHandler` deve publicar o evento `TransactionNormalized` via **MassTransit Transactional Outbox**, garantindo entrega at-least-once sem dual-write e sem acoplamento da camada `Application` ao MassTransit.

---

## 2. Contexto

| Item | Estado |
|------|--------|
| `TransactionNormalized` event record | ✅ Existe em `Shared.Messaging` |
| `TransactionIngested` event record | ✅ Existe em `Shared.Messaging` |
| `AddFinanceHubMessaging()` | ✅ Existe, mas tem magic strings a corrigir |
| `IngestTransactionCommandHandler` | ✅ Existe — não injeta `IBus` ainda |
| EF Core Outbox configurado | ❌ Não configurado |
| `IEventPublisher` na Application | ❌ Não existe |

---

## 3. Hierarquia de Eventos (Open/Closed Principle)

Adotada hierarquia OCP em `Shared.Messaging/Events/` em vez de um record plano com todos os campos. Cada novo banco ou contexto **estende** a base sem modificá-la.

```csharp
public record TransactionNormalized(
    Guid TransactionId,
    string Source,
    string AccountId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc) : IFinanceHubEvent;

public record BankTransactionNormalized(
    Guid TransactionId,
    string Source,
    string AccountId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc,
    Guid IngestionId,
    string? RawPayloadJson)
    : TransactionNormalized(TransactionId, Source, AccountId, Amount, Currency,
                            TransactionType, TransactionDate, CleanDescription,
                            HashDeduplicacao, ProcessedAtUtc);

public record TransactionCategorized(
    Guid TransactionId,
    Guid CategoryId,
    string CategoryName,
    string CategorizationSource,
    DateTime CategorizedAtUtc) : IFinanceHubEvent;
```

**Motivação:** `IngestionId` só existe via conector de banco. `Category` pode ser resolvida assincronamente. Cada consumer escolhe qual contrato consumir sem acoplamento a campos fora do seu contexto.

---

## 4. Fluxo Arquitetural Alvo

```
IngestTransactionCommandHandler (Application)
    │
    ├─ ITransactionRepository.AddAsync()         ← persiste no PostgreSQL
    ├─ IAccountBalanceRepository.AddOrUpdateAsync()
    └─ IEventPublisher.PublishAsync<TransactionNormalized>()  ← publica via Outbox
                │
                ▼
        EventPublisher (Infrastructure)
                │
                ▼
        IBus.Publish() → MassTransit Outbox → RabbitMQ
```

---

## 5. Decisões em Aberto

### 5.1 — Mapeamento do campo `IngestionId`
O `TransactionNormalized.IngestionId` é do tipo `Guid`. O `IngestTransactionCommand` atual **não carrega um `IngestionId`** explícito — ele vem do conector de banco (Itaú, MP). Para ingestão direta via REST (cenário atual), precisamos definir como preencher este campo.

> ✅ **DECIDIDO**: Gerar `Guid.NewGuid()` no momento da ingestão REST — representa o ID único desta operação de ingestão. Quando os conectores de banco (Itaú, MP) forem implementados, eles preencherão o `IngestionId` com seu próprio identificador de operação.

### 5.2 — Configuração do Outbox
O MassTransit suporta Outbox via EF Core (`AddEntityFrameworkOutbox`) ou via memória (`UseInMemoryOutbox` — apenas para testes). A configuração produtiva usará PostgreSQL.

> ✅ **DECIDIDO**: O Outbox será registrado dentro de `AddTransactionAggregatorInfrastructureServices()` na Infrastructure DI — encapsulado, seguindo a Regra 11 do `AGENTS.md`. O `Program.cs` apenas orquestra chamando o método de extensão.

### 5.3 — Fix de Magic Strings em `MessagingExtensions`
O arquivo atual tem `?? "localhost"`, `?? "guest"`, etc. que violam a Rule 12 (`ENV-001`).

> **Decisão**: corrigir com `throw new InvalidOperationException(...)` — consenso do plano.

---

## 6. Perguntas de Decisão

- [x] **Pergunta 1**: `IngestionId` — `Guid.NewGuid()` no REST / do conector em ingestões via banco ✅
- [x] **Pergunta 2**: Outbox registrado em `AddTransactionAggregatorInfrastructureServices()` ✅
- [x] **Pergunta 3**: Hierarquia OCP de eventos — `TransactionNormalized` + `BankTransactionNormalized` + `TransactionCategorized` ✅

---

## 7. Casos de Teste (TDD — Red First)

| # | Cenário | Tipo | Status |
|---|---------|------|--------|
| T1 | Nova transação → `TransactionNormalized` publicado 1x com dados corretos | Positivo | ❌ A escrever |
| T2 | Transação duplicada (hash existe) → evento NÃO publicado | Negativo | ❌ A escrever |
| T3 | `IEventPublisher.PublishAsync` lança exceção → handler propaga sem swallow | Borda | ❌ A escrever |

---

## 8. Critérios de Aceite

- [ ] `IFinanceHubEvent` marker interface em `Shared.Messaging`
- [ ] `TransactionNormalized`, `BankTransactionNormalized`, `TransactionCategorized` em arquivos separados
- [ ] `IEventPublisher` reside em `Application.Interfaces` — zero referência a MassTransit na camada Application
- [ ] `EventPublisher` reside em `Infrastructure.Messaging` — implementa `IEventPublisher` via `IBus`
- [ ] `IngestTransactionCommandHandler` publica `TransactionNormalized` (base) via REST
- [ ] `MessagingExtensions` sem nenhum `??` com valor default — fail-fast
- [ ] Outbox registrado via `AddEntityFrameworkOutbox<TransactionAggregatorDbContext>`
- [ ] `dotnet test` → todos os testes passando (T1–T5 do RED)
- [ ] `dotnet build` → 0 erros
