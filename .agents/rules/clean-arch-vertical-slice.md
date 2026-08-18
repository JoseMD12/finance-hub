# Clean Architecture + Microservices Rules — FinanceHub (.NET 10)

## 1. Microservices Ecosystem & Boundaries
FinanceHub is structured into autonomous microservices (`src/Services/`) and shared modules (`src/Shared/`).

### Service Boundaries & Rules
1. **`FinanceHub.PluggyIntegration`**: Unified Open Finance connector for Brazilian banks (Itaú, Inter, Mercado Pago) via Meu.Pluggy API. Emits `TransactionIngested` and `InvoiceItemIngested` events.
2. **`FinanceHub.FileImporter`**: Offline financial file ingestion engine for `.ofx`, `.csv`, and `.pdf` bank/card statements.
3. **`FinanceHub.TransactionAggregator`**: Consumes `TransactionIngested` / `InvoiceItemIngested`, normalizes to canonical ledger model, deduplicates (SHA-256), and persists history. Emits `TransactionNormalized`.
4. **`FinanceHub.ApiGateway`**: Single entrypoint BFF for frontend web application (`FinanceHub.Web`). No direct DB access — calls services via internal API.
5. **`FinanceHub.Web`**: React 19 + Vite + TailwindCSS SPA user interface consuming `FinanceHub.ApiGateway`.

## 2. Microservices Strict Constraints
- **Database Isolation**: Each service owns its database. Direct database calls to another service's DB are STRICTLY PROHIBITED.
- **Async Communication**: Inter-service events use RabbitMQ / MassTransit with the Transactional Outbox Pattern.
- **No Integration Coupling**: Services interact asynchronously via events or HTTP BFF routing.
- **Shared Libraries**: Use `FinanceHub.Shared.Messaging` and `FinanceHub.Shared.Observability`. No business logic allowed in `Shared.*`.

## 4. Inversão de Dependência Estrita (DIP) em Use Cases & Handlers
- **Regra de Ouro**: TODOS os Handlers de Command e Query DEVEM implementar uma interface dedicada (ex: `ISyncAllPluggyAccountsCommandHandler`, `ISyncPluggyAccountCommandHandler`, `IGetAggregatedDashboardQueryHandler`).
- **Exceção Única**: Apenas classes estáticas (métodos de extensão, utilitários puros sem estado) são isentas de interfaces.
- **Injeção em Endpoints**: Endpoints de API DEVEM receber obrigatoriamente a interface do Handler em seus parâmetros (ex: `ISyncAllPluggyAccountsCommandHandler handler`), aplicando o princípio de Inversão de Dependência (DIP) em 100% da solução.

## 5. Arquivos Separados Obrigatórios para Interface e Implementação

**PROIBIDO**: Declarar `public interface I<Name>` e `public class <Name>` no mesmo arquivo `.cs`.

Cada interface de Handler/Service e sua implementação DEVEM residir em arquivos `.cs` dedicados e separados dentro da mesma pasta de Use Case:

```text
Commands/SyncAllPluggyAccounts/
  ├── SyncAllPluggyAccountsCommand.cs              ← record Command
  ├── ISyncAllPluggyAccountsCommandHandler.cs      ← interface (contrato)
  └── SyncAllPluggyAccountsCommandHandler.cs       ← class (implementação)

Queries/GetAggregatedDashboard/
  ├── GetAggregatedDashboardQuery.cs              ← record Query
  ├── IGetAggregatedDashboardQueryHandler.cs      ← interface (contrato)
  └── GetAggregatedDashboardQueryHandler.cs       ← class (implementação)
```

O padrão de referência canônico do projeto é `FinanceHub.PluggyIntegration.Application`. **Qualquer violação desta regra é tratada como falha crítica de arquitetura (DIP-001).**

## 6. Hierarquia OCP de Eventos em `Shared.Messaging`

### Princípio
Eventos em `FinanceHub.Shared.Messaging` seguem o **Open/Closed Principle**: a base canônica é fechada para modificação; novos bancos, conectores ou contextos adicionam subtipos sem alterar contratos existentes.

### Estrutura Obrigatória de Arquivos

```text
FinanceHub.Shared.Messaging/Events/
  ├── IFinanceHubEvent.cs                  ← marker interface
  ├── TransactionNormalized.cs             ← base canônica (campos sempre presentes)
  ├── BankTransactionNormalized.cs         ← extensão para conectores de banco
  └── TransactionCategorized.cs            ← evento dedicado à categorização
```

### Contratos

```csharp
public interface IFinanceHubEvent { }

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

### Regras de Uso
- `TransactionNormalized` é publicado pelo `TransactionAggregator` após ingestão REST direta.
- `BankTransactionNormalized` é publicado pelos conectores de banco (Itaú, Mercado Pago, Inter) — carrega `IngestionId` e payload original.
- `TransactionCategorized` é publicado quando a categorização ocorre de forma assíncrona ou é atualizada manualmente.
- Consumers devem depender do contrato mais específico que precisam; nunca realizar cast entre tipos de evento.
- **Proibido**: adicionar campos opcionais ou específicos de banco diretamente em `TransactionNormalized`. Criar um subtipo.

