# Especificação Técnica: Otimização de Performance, Sincronização Assíncrona, Resiliência DLQ e Concorrência Otimista

**Documento:** `.agents/specs/sync-performance-and-resilience-spec.md`  
**Status:** 🟢 `Aprovada para Implementação`  
**Data:** 19/08/2026 (Atualizado em 20/08/2026)  
**Escopo:** `FinanceHub.PluggyIntegration`, `FinanceHub.TransactionAggregator`, `FinanceHub.Shared.Messaging`, `FinanceHub.ApiGateway`, `FinanceHub.Web`

---

## 1. 🎯 Objetivo & Visão Geral

Implementar o pacote de otimizações de performance, assincronismo, resiliência e integridade transacional no fluxo de sincronização Open Finance e ingestão de transações:

1. **Ingestão em Lote & Chunking (`TransactionsBatchIngested` e `PublishBatch`)**:
   - Eliminar publicação e inserção de eventos item-a-item no pipeline de sincronização.
2. **Sincronização Assíncrona no Endpoint REST (`POST /api/v1/pluggy/sync`) com Job Polling (`GET /jobs/{id}`)**:
   - Responder imediatamente `202 Accepted` com `JobId`.
   - Disponibilizar consulta de status do job em memória (`ISyncJobStore`) até conclusão (`Completed`) com resumo real de entidades sincronizadas.
3. **Gateway BFF & Frontend Web Integration**:
   - `ApiGateway`: Repasse transparente de `POST /sync`, `GET /sync/jobs/{id}` e `GET /accounts`.
   - `FinanceHub.Web`: Polling assíncrono via TanStack Query em `useSyncPluggyMutation`, atualizando banner e notificações sem bloqueio.
4. **Concorrência Otimista (PostgreSQL `xmin` RowVersion)**:
   - Garantir controle de concorrência em `CanonicalTransaction` e `UserCategoryRule`.
5. **Dead Letter Queue (DLQ) & Retry Exponencial em Mensageria**:
   - Configurar resiliência com política de retry e filas `_error`/`_skipped` no MassTransit.

---

## 2. 🏛️ Decisões Arquiteturais Confirmadas

### 2.1 Decisão 1: Ingestão em Lote e Chunking de Transações
- **Escolha**: Evento de Lote Dedicado (`TransactionsBatchIngested`) com Chunking de 50 itens.
- **Detalhamento**:
  - `FinanceHub.PluggyIntegration`: No `SyncAllPluggyAccountsCommandHandler`, as transações mapeadas (contas correntes e cartões) serão divididas em lotes de no máximo 50 itens por mensagem (`.Chunk(50)`).
  - Cada lote será publicado via `publishEndpoint.PublishBatch` utilizando o evento `TransactionsBatchIngested`.
  - `FinanceHub.TransactionAggregator`: O consumidor `TransactionsBatchIngestedConsumer` processa o lote de forma atômica:
    1. Extrai a lista de hashes de todas as transações do lote.
    2. Executa **1 única consulta SQL de deduplicação** via EF Core: `_context.Transactions.Where(t => batchHashes.Contains(t.Hash))`.
    3. Adiciona todas as transações inéditas via `AddRangeAsync`.
    4. Atualiza os saldos e persiste no banco em **1 única transação atômica (`SaveChangesAsync`)**.

### 2.2 Decisão 2: Resposta Assíncrona no Endpoint REST (`POST /sync`) e Job Status (`GET /jobs/{id}`)
- **Escolha**: Resposta `202 Accepted` imediata por padrão + In-Memory Job Store (`ISyncJobStore`).
- **Detalhamento**:
  - Ao receber a chamada em `POST /api/v1/pluggy/sync`, o controlador/endpoint valida os cabeçalhos (`UserId` e `X-Pluggy-Access-Token`), gera um `JobId` único (`Guid`), cadastra o job em `ISyncJobStore` com status `Processing` e retorna imediatamente `202 Accepted` com `SyncJobAcceptedDto(JobId, Status: "Processing", Message, StartedAtUtc)`.
  - A execução de sincronização (`ISyncAllPluggyAccountsCommandHandler`) roda em segundo plano (`Task.Run`). Ao finalizar com sucesso, atualiza o job para `Completed` com o `SyncPluggySummaryDto` resultante. Em caso de falha, atualiza para `Failed`.
  - Novo endpoint `GET /api/v1/pluggy/sync/jobs/{jobId}` retorna `SyncJobStatusDto` para consulta de progresso.
  - Futura migração planejada para Redis sem alteração no contrato do endpoint.

### 2.3 Decisão 3: Orquestração no BFF ApiGateway e Consumo no Frontend Web
- **Escolha**: Gateway com DTOs tipados e Polling leve no TanStack Query.
- **Detalhamento**:
  - `ApiGateway`: Expõe `POST /api/v1/gateway/pluggy/sync` (retornando `202 Accepted` com `GatewaySyncJobAcceptedDto`) e `GET /api/v1/gateway/pluggy/sync/jobs/{jobId}` (retornando `GatewaySyncJobStatusDto`).
  - `ApiGateway`: Expõe `GET /api/v1/gateway/pluggy/accounts` e `GET /api/v1/pluggy/accounts` para consulta de contas conectadas pela extensão e frontend.
  - `FinanceHub.Web`: `useSyncPluggyMutation` dispara o sync, exibe toast informativo imediato (*"Sincronização iniciada em segundo plano..."*), realiza polling em intervalos de 1s até `status === 'Completed'`, grava o `SyncPluggySummaryDto` no `sessionStorage` e dispara as invalidações de cache do TanStack Query.

### 2.4 Decisão 4: Dead Letter Queue (DLQ) & Retry Exponencial em Mensageria
- **Escolha**: Configuração Global em `MessagingExtensions.cs`.
- **Detalhamento**:
  - No módulo `FinanceHub.Shared.Messaging`, a extensão `AddFinanceHubMessaging` configura o pipeline do RabbitMQ com retry exponencial centralizado: `cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)))`.
  - Se uma mensagem falhar em todas as 5 tentativas de processamento, o MassTransit move automaticamente a mensagem para a fila de quarentena de erro (`<queue-name>_error` / `<queue-name>_skipped`).

### 2.5 Decisão 5: Concorrência Otimista (PostgreSQL `xmin` System Column)
- **Escolha**: Aplicar `xmin` em todas as entidades principais do Aggregator (`CanonicalTransaction`, `AccountBalance` e `UserCategoryRule`).
- **Detalhamento**:
  - No `FinanceHub.TransactionAggregator.Infrastructure`, mapear o token de concorrência nativo do PostgreSQL (`xmin`) em `CanonicalTransactionConfiguration` e `UserCategoryRuleConfiguration` (além de `AccountBalanceConfiguration`, que já o possui).

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

### 3.2 DTOs de Job Assíncrono (`FinanceHub.PluggyIntegration.Application.DTOs`)
```csharp
namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record SyncJobAcceptedDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc
);

public record SyncJobStatusDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    SyncPluggySummaryDto? Result,
    string? ErrorMessage
);
```

### 3.3 DTOs do BFF ApiGateway (`FinanceHub.ApiGateway.DTOs`)
```csharp
namespace FinanceHub.ApiGateway.DTOs;

public record GatewaySyncJobAcceptedDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc
);

public record GatewaySyncJobStatusDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    GatewayPluggySyncSummaryDto? Result,
    string? ErrorMessage
);
```

---

## 4. 🧪 Plano de Implementação & Checklist de Validação

- [ ] **Fase 1: PluggyIntegration Job Store & Endpoints**
  - [ ] Implementar interface `ISyncJobStore.cs` e `InMemorySyncJobStore.cs` em `FinanceHub.PluggyIntegration.Application`.
  - [ ] Registrar `ISyncJobStore` como Singleton no DI (`DependencyInjection.cs`).
  - [ ] Em `PluggyEndpoints.cs`:
    - `POST /api/v1/pluggy/sync` cria o Job como `Processing`, dispara `Task.Run` e retorna `202 Accepted` (`SyncJobAcceptedDto`).
    - `GET /api/v1/pluggy/sync/jobs/{jobId}` retorna `200 OK` (`SyncJobStatusDto`) ou `404 Not Found`.
- [ ] **Fase 2: BFF ApiGateway Clientes & Endpoints**
  - [ ] Atualizar `IPluggyIntegrationServiceClient` e `PluggyIntegrationServiceClient`:
    - `TriggerSyncAsync` retorna `GatewaySyncJobAcceptedDto`.
    - Adicionar `GetSyncJobStatusAsync(jobId)` retornando `GatewaySyncJobStatusDto`.
    - Adicionar `GetAccountsAsync(token)` retornando `GatewayPluggyAccountDto[]`.
  - [ ] Atualizar `PluggyGatewayEndpoints.cs`:
    - Mapear `POST /api/v1/gateway/pluggy/sync` (`202 Accepted`).
    - Mapear `GET /api/v1/gateway/pluggy/sync/jobs/{jobId}` (`200 OK`).
    - Mapear `GET /api/v1/gateway/pluggy/accounts` e `GET /api/v1/pluggy/accounts` (`200 OK`).
- [ ] **Fase 3: Frontend Web Polling & Invalidação**
  - [ ] Atualizar `connections.types.ts` e `connectionsApi.ts` com `SyncJobAcceptedDto` e `getSyncJobStatusApi`.
  - [ ] Refatorar `useSyncPluggyMutation.ts` para realizar polling com timeout até `Completed`, gravando o resultado real no `sessionStorage` e disparando as invalidações do TanStack Query.
- [ ] **Fase 4: Testes & Validação**
  - [ ] Atualizar testes unitários em `FinanceHub.Tests` para validar o fluxo assíncrono e o status do job.
  - [ ] Atualizar testes de frontend em `FinanceHub.Web`.
  - [ ] Executar `dotnet test`, `npm test` e testar via script manual de API curl.
