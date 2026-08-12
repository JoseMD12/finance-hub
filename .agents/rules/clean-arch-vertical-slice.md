# Clean Architecture + Microservices Rules — FinanceHub (.NET 10)

## 1. Microservices Ecosystem & Boundaries
FinanceHub is structured into autonomous microservices (`src/Services/`) and shared modules (`src/Shared/`).

### Service Boundaries & Rules
1. **`FinanceHub.AuthConsent`**: OAuth2/OIDC + FAPI flow per bank institution. Manages consent, tokens (`access_token`, `refresh_token`). Exposes internal API for valid tokens.
2. **`FinanceHub.ItauIntegration`**: Consumes Itaú Open Finance API. Translates response to `TransactionIngested` event (`source: "Itau"`). No direct link to Mercado Pago or Inter.
3. **`FinanceHub.MercadoPagoIntegration`**: Same as Itaú, isolated for Mercado Pago.
4. **`FinanceHub.InterIntegration`**: Banco Inter API connector (to implement after Inter phase confirmation).
5. **`FinanceHub.TransactionAggregator`**: Consumes `TransactionIngested` from any bank, normalizes to canonical model, deduplicates, and persists history. Emits `TransactionNormalized`.
6. **`FinanceHub.ApiGateway`**: Single entrypoint BFF for frontend/app. No direct DB access — calls services via internal API.

## 2. Microservices Strict Constraints
- **Database Isolation**: Each service owns its database. Direct database calls to another service's DB are STRICTLY PROHIBITED.
- **Async Communication**: Inter-service events use RabbitMQ / MassTransit with the Transactional Outbox Pattern.
- **No Integration Coupling**: Never reference one Bank Integration Service from another.
- **Shared Libraries**: Use `FinanceHub.Shared.Certificates`, `FinanceHub.Shared.Messaging`, and `FinanceHub.Shared.Observability`. No business logic allowed in `Shared.*`.

## 4. Inversão de Dependência Estrita (DIP) em Use Cases & Handlers
- **Regra de Ouro**: TODOS os Handlers de Command e Query DEVEM implementar uma interface dedicada (ex: `IAuthorizeConsentCommandHandler`, `ICreateConsentCommandHandler`, `IRenewTokenCommandHandler`, `IRevokeConsentCommandHandler`, `IGetConsentByUserIdQueryHandler`).
- **Exceção Única**: Apenas classes estáticas (métodos de extensão, utilitários puros sem estado) são isentas de interfaces.
- **Injeção em Endpoints**: Endpoints de API DEVEM receber obrigatoriamente a interface do Handler em seus parâmetros (ex: `IAuthorizeConsentCommandHandler handler`), aplicando o princípio de Inversão de Dependência (DIP) em 100% da solução.

## 5. Arquivos Separados Obrigatórios para Interface e Implementação

**PROIBIDO**: Declarar `public interface I<Name>` e `public class <Name>` no mesmo arquivo `.cs`.

Cada interface de Handler/Service e sua implementação DEVEM residir em arquivos `.cs` dedicados e separados dentro da mesma pasta de Use Case:

```text
Commands/AuthorizeConsent/
  ├── AuthorizeConsentCommand.cs              ← record Command
  ├── IAuthorizeConsentCommandHandler.cs      ← interface (contrato)
  └── AuthorizeConsentCommandHandler.cs       ← class (implementação)

Queries/GetConsentByUserId/
  ├── GetConsentByUserIdQuery.cs              ← record Query
  ├── IGetConsentByUserIdQueryHandler.cs      ← interface (contrato)
  └── GetConsentByUserIdQueryHandler.cs       ← class (implementação)
```

O padrão de referência canônico do projeto é `FinanceHub.AuthConsent.Application`. **Qualquer violação desta regra é tratada como falha crítica de arquitetura (DIP-001).**

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

