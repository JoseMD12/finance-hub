# Especificação Técnica: Otimização de Performance, Sincronização Assíncrona, Resiliência DLQ e Concorrência Otimista

**Documento:** `.agents/specs/sync-performance-and-resilience-spec.md`  
**Status:** 🟢 `Aprovada para Implementação`  
**Data:** 19/08/2026  
**Escopo:** `FinanceHub.PluggyIntegration`, `FinanceHub.TransactionAggregator`, `FinanceHub.Shared.Messaging`, `FinanceHub.ApiGateway`

---

## 1. 🎯 Objetivo & Visão Geral

Implementar o pacote de otimizações de performance, assincronismo, resiliência e integridade transacional no fluxo de sincronização Open Finance e ingestão de transações:

1. **Ingestão em Lote & Chunking (`TransactionsBatchIngested` e `PublishBatch`)**:
   - Eliminar publicação e inserção de eventos item-a-item no pipeline de sincronização.
2. **Sincronização Assíncrona no Endpoint REST (`POST /api/v1/pluggy/sync`)**:
   - Alterar o endpoint para responder imediatamente `202 Accepted` com `JobId`.
3. **Concorrência Otimista (PostgreSQL `xmin` RowVersion)**:
   - Garantir controle de concorrência em `CanonicalTransaction` e `UserCategoryRule`.
4. **Dead Letter Queue (DLQ) & Retry Exponencial em Mensageria**:
   - Configurar resiliência com política de retry e filas `_error`/`_skipped` no MassTransit.

---

## 2. 🏛️ Decisões Arquiteturais Confirmadas

### 2.1 Decisão 1: Ingestão em Lote e Chunking de Transações
- **Escolha**: Evento de Lote Dedicado (`TransactionsBatchIngested`) com Chunking de 50 itens.
- **Detalhamento**:
  - `FinanceHub.PluggyIntegration`: No `SyncAllPluggyAccountsCommandHandler`, as transações mapeadas (contas correntes e cartões) serão divididas em lotes de no máximo 50 itens por mensagem (`.Chunk(50)`).
  - Cada lote será publicado via `publishEndpoint.PublishBatch` utilizando o novo evento `TransactionsBatchIngested`.
  - `FinanceHub.TransactionAggregator`: O novo consumidor `TransactionsBatchIngestedConsumer` processará o lote de forma atômica:
    1. Extrai a lista de hashes de todas as transações do lote.
    2. Executa **1 única consulta SQL de deduplicação** via EF Core: `_context.Transactions.Where(t => batchHashes.Contains(t.Hash))`.
    3. Adiciona todas as transações inéditas via `AddRangeAsync`.
    4. Atualiza os saldos e persiste no banco em **1 única transação atômica (`SaveChangesAsync`)**.

### 2.2 Decisão 2: Resposta Assíncrona no Endpoint REST (`POST /api/v1/pluggy/sync`)
- **Escolha**: Resposta `202 Accepted` imediata por padrão.
- **Detalhamento**:
  - Ao receber a chamada no endpoint `POST /api/v1/pluggy/sync`, o controlador/endpoint valida os cabeçalhos (`UserId` e `X-Pluggy-Access-Token`), gera um `JobId` único (`Guid`) e retorna imediatamente `202 Accepted` com `SyncJobAcceptedDto(JobId, Status: "Processing", Message: "...", StartedAtUtc)`.
  - A execução da sincronização (`ISyncAllPluggyAccountsCommandHandler`) é disparada em segundo plano.
  - O cliente REST (ou ApiGateway/Frontend) é liberado em poucos milissegundos sem travar aguardando as chamadas externas do Meu.Pluggy.

### 2.3 Decisão 3: Dead Letter Queue (DLQ) & Retry Exponencial em Mensageria
- **Escolha**: Configuração Global em `MessagingExtensions.cs`.
- **Detalhamento**:
  - No módulo `FinanceHub.Shared.Messaging`, a extensão `AddFinanceHubMessaging` configura o pipeline do RabbitMQ com retry exponencial centralizado: `cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)))`.
  - Se uma mensagem falhar em todas as 5 tentativas de processamento (ex: indisponibilidade temporária de banco de dados ou erro de concorrência), o MassTransit move automaticamente a mensagem para a fila de quarentena de erro (`<queue-name>_error` / `<queue-name>_skipped`).
  - Isso garante resiliência e isolamento completo de mensagens corrompidas (*poison messages*) sem bloquear a fila principal.

### 2.4 Decisão 4: Concorrência Otimista (PostgreSQL `xmin` System Column)
- **Escolha**: Aplicar `xmin` em todas as entidades principais do Aggregator (`CanonicalTransaction`, `AccountBalance` e `UserCategoryRule`).
- **Detalhamento**:
  - No `FinanceHub.TransactionAggregator.Infrastructure`, mapear o token de concorrência nativo do PostgreSQL (`xmin`) em `CanonicalTransactionConfiguration` e `UserCategoryRuleConfiguration` (além de `AccountBalanceConfiguration`, que já o possui):
    ```csharp
    builder.Property<uint>("xmin")
        .HasColumnName("xmin")
        .HasColumnType("xid")
        .ValueGeneratedOnAddOrUpdate()
        .IsRowVersion();
    ```
  - Em cenários de updates concorrentes simultâneos entre Webhooks e sincronizações manuais, o EF Core lançará `DbUpdateConcurrencyException`, impedindo atualizações sobrescritas (*lost updates*).


---

## 3. 🧩 Contratos de DTOs & Eventos de Domínio

### 3.1 Evento de Ingestão em Lote (`FinanceHub.Shared.Messaging.Events.TransactionsBatchIngested`)
```csharp
namespace FinanceHub.Shared.Messaging.Events;

public record TransactionsBatchIngested(
    Guid BatchId,
    string? UserId,
    int ChunkIndex,
    int TotalChunks,
    IReadOnlyList<TransactionIngested> CheckingTransactions,
    IReadOnlyList<InvoiceItemIngested> CardTransactions,
    DateTime OccurredAtUtc
) : IFinanceHubEvent;
```

### 3.2 DTO de Resposta Assíncrona (`FinanceHub.PluggyIntegration.Application.DTOs.SyncJobAcceptedDto`)
```csharp
namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record SyncJobAcceptedDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc
);
```

---

## 4. 🧪 Plano de Implementação & Checklist de Validação

- [ ] **Fase 1: Mensageria & Shared Contracts**
  - [ ] Criar evento `TransactionsBatchIngested.cs` em `FinanceHub.Shared.Messaging/Events/`.
  - [ ] Configurar `cfg.UseMessageRetry` em `MessagingExtensions.cs`.
- [ ] **Fase 2: PluggyIntegration Service**
  - [ ] Criar `SyncJobAcceptedDto.cs`.
  - [ ] Atualizar `SyncAllPluggyAccountsCommandHandler.cs` para agrupar em chunks (`.Chunk(50)`) e publicar `TransactionsBatchIngested` via `publishEndpoint.PublishBatch`.
  - [ ] Refatorar endpoint `POST /api/v1/pluggy/sync` em `PluggyEndpoints.cs` para retornar `202 Accepted`.
- [ ] **Fase 3: TransactionAggregator Service**
  - [ ] Criar consumidor `TransactionsBatchIngestedConsumer.cs` com busca em lote de hashes (`Where(t => batchHashes.Contains(t.Hash))`) e `AddRangeAsync`.
  - [ ] Mapear `xmin` em `CanonicalTransactionConfiguration.cs` e `UserCategoryRuleConfiguration.cs`.
- [ ] **Fase 4: Validação & Suíte de Testes**
  - [ ] Atualizar testes unitários em `SyncAllPluggyAccountsCommandHandlerTests.cs` e criar testes para `TransactionsBatchIngestedConsumerTests`.
  - [ ] Executar `dotnet build` e `dotnet test` (garantindo 100% de testes passantes).




