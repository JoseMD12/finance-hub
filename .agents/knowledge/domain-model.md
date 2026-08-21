# FinanceHub - Domain Model Specification (.NET 10 Microservices / DDD)

This document defines the core Domain Driven Design (DDD) model for **FinanceHub**, built on **.NET 10** and **C# 13**. It outlines Aggregates, Entities, Value Objects, Domain/Integration Events, and State Transitions, mapped cleanly across FinanceHub microservices boundaries.

---

## 🏛️ Microservice Domain Boundaries & Database Isolation

FinanceHub enforces **Database-per-Service** isolation. Each aggregate resides strictly within its owning microservice:

- **`FinanceHub.PluggyIntegration`**: Manages Open Finance personal bank connections via Meu.Pluggy (covering Itaú, Banco Inter, and Mercado Pago), emits `TransactionIngested` and `InvoiceItemIngested`.
- **`FinanceHub.FileImporter`**: Offline ingestion engine for `.ofx`, `.csv`, and `.pdf` files.
- **`FinanceHub.TransactionAggregator`**: Manages core financial domain models: `ContaFinanceira`, `Instituicao`, `CanonicalTransaction` (`Transacao`), `Category` (`Categoria`), and `Orcamento`.
- **`FinanceHub.ApiGateway`**: BFF handling client authentication, rate limiting, and route orchestration.

> **Historical Architecture Note**: Legacy direct bank connectors (`AuthConsent`, `ItauIntegration`, `MercadoPagoIntegration`, `InterIntegration`) and `Shared.Certificates` have been consolidated into `PluggyIntegration` and `FileImporter` (see [System Architecture ADR](system-architecture-and-services.md)).

---

## 1. Aggregates & Entities

### 1.1 `ContaFinanceira` (Aggregate Root — `TransactionAggregator`)
Represents a financial account (Checking, Savings, Investment, Credit Card) owned by a user.
- **Properties**:
  - `Id`: `ContaId` (Strongly typed Guid)
  - `UsuarioId`: `UsuarioId`
  - `InstituicaoId`: `InstituicaoId`
  - `Nome`: `string`
  - `TipoConta`: `TipoContaEnum` (`Corrente`, `Poupanca`, `Investimento`, `CartaoCredito`)
  - `SaldoAtual`: `Money`
  - `Moeda`: `Currency`
  - `Status`: `StatusContaEnum` (`Ativa`, `Inativa`, `Bloqueada`, `Arquivada`)
  - `ConexaoOpenFinanceId`: `ConexaoId?`
  - `CriadoEm`: `DateTimeOffset`
  - `AtualizadoEm`: `DateTimeOffset`
- **Domain Invariants**:
  - A financial account must be associated with a valid user and institution.
  - Credit limit accounts can have negative balances down to their credit limit.
  - Deactivating an account prevents new manual or automated transactions.

### 1.2 `Instituicao` (Aggregate Root / Entity — `TransactionAggregator`)
Represents a banking or financial institution participating in Open Finance or custom connectors.
- **Properties**:
  - `Id`: `InstituicaoId`
  - `CodigoCompe`: `string` (e.g., "341" for Itaú, "077" for Inter)
  - `Nome`: `string`
  - `ISPB`: `string`
  - `UrlLogo`: `Uri`
  - `SuportaOpenFinance`: `bool`
  - `TipoConector`: `TipoConectorEnum` (`OpenFinanceBrasil`, `ItauDirect`, `BancoInterDirect`, `MercadoPagoApi`, `Manual`)

### 1.3 `Transacao` (Aggregate Root — `TransactionAggregator`)
Represents a monetary entry (Debit or Credit) recorded in an account.
- **Properties**:
  - `Id`: `TransacaoId`
  - `ContaId`: `ContaId`
  - `CategoriaId`: `CategoriaId?`
  - `Valor`: `Money`
  - `Tipo`: `TipoTransacaoEnum` (`Debito`, `Credito`)
  - `Status`: `StatusTransacaoEnum` (`Pendente`, `Concluida`, `Cancelada`, `Estornada`)
  - `DescricaoOriginal`: `string`
  - `DescricaoPersonalizada`: `string?`
  - `DataTransacao`: `DateTimeOffset`
  - `DataEfetivacao`: `DateTimeOffset?`
  - `HashUnico`: `string` (Calculated for deduplication: SHA256 of AccountId + Date + Amount + OriginalDescription + ExternalId)
  - `ExternalId`: `string?` (ID provided by Open Finance / Bank API)
  - `Metadados`: `Dictionary<string, string>` (Store raw categorization signals, merchant details, etc.)

### 1.4 `Categoria` (Entity / Aggregate Root — `TransactionAggregator`)
Defines transaction classification for budgeting and reporting.
- **Properties**:
  - `Id`: `CategoriaId`
  - `UsuarioId`: `UsuarioId?` (Null for system-default categories)
  - `CategoriaPaiId`: `CategoriaId?` (Hierarchical categories)
  - `Nome`: `string`
  - `CorHex`: `string`
  - `Icone`: `string`
  - `Tipo`: `TipoTransacaoEnum` (`Debito`, `Credito`, `Ambos`)

### 1.5 `Orcamento` (Aggregate Root — `TransactionAggregator`)
Tracks spending limits per category for a specified time frame.
- **Properties**:
  - `Id`: `OrcamentoId`
  - `UsuarioId`: `UsuarioId`
  - `CategoriaId`: `CategoriaId`
  - `LimiteValor`: `Money`
  - `ValorGastoAtual`: `Money`
  - `Periodo`: `Periodo` (Value Object: StartDate, EndDate)
  - `Notificado80Percento`: `bool`
  - `NotificadoExcedido`: `bool`
- **Domain Methods**:
  - `AcumularGasto(Money valor)`: Recalculates current spending and raises alerts if thresholds (80%, 100%) are passed.

### 1.6 `ConexaoOpenFinance` (Aggregate Root / Entity — `PluggyIntegration`)
Manages Open Finance connection status, item synchronization timestamps, and account mappings.
- **Properties**:
  - `Id`: `ConexaoId`
  - `UsuarioId`: `UsuarioId`
  - `InstituicaoId`: `InstituicaoId`
  - `ItemId`: `string` (External Pluggy item ID)
  - `Status`: `StatusConexaoEnum` (`UPDATED`, `UPDATING`, `WAITING_USER_INPUT`, `LOGIN_ERROR`)
  - `UltimaSincronizacao`: `DateTimeOffset?`

---

## 2. Value Objects

### 2.1 `Money`
- **Fields**: `decimal Amount`, `Currency Currency`
- **Rules**:
  - Immutable struct/record.
  - Supports operations: `Add`, `Subtract`, `Multiply`, `PercentageOf`.
  - Throws `CurrencyMismatchException` if operating on different currencies.

### 2.2 `Currency`
- **Fields**: `string Code` (ISO 4217, e.g., "BRL", "USD"), `string Symbol` ("R$"), `int DecimalPlaces` (2)
- **Predefined**: `Currency.BRL`, `Currency.USD`, `Currency.EUR`.

### 2.3 `BankCredentials`
- **Fields**: `string RefreshTokenEncrypted`, `string AccessTokenEncrypted`, `DateTimeOffset ExpiresAt`, `string Scopes`
- **Rules**:
  - Stored encrypted at rest using AES-256-GCM via Data Protection API / KMS integration.
  - Never exposed directly outside infrastructural boundary or logged.

### 2.4 `Periodo`
- **Fields**: `DateOnly DataInicio`, `DateOnly DataFim`
- **Rules**: `DataFim` must be greater than or equal to `DataInicio`. Provides `Contem(DateOnly data)` logic.

---

## 3. Domain & Integration Events

| Event Name | Type | Emitted By | Key Payload |
|---|---|---|---|
| `TransacaoCriadaEvent` | Domain Event | `TransactionAggregator` | `TransacaoId`, `ContaId`, `Money`, `DataTransacao`, `HashUnico` |
| `TransactionIngested` | Integration Event | `PluggyIntegration` / `FileImporter` | `ExternalAccountId`, `BankCode`, `Amount`, `Description`, `RawPayload`, `IngestedAt` |
| `InvoiceItemIngested` | Integration Event | `PluggyIntegration` / `FileImporter` | `CreditCardAccountId`, `Amount`, `Description`, `Category`, `DueDate` |
| `TransactionsBatchIngested` | Integration Event | `PluggyIntegration` | `UserId`, `Transactions`, `BatchId`, `PublishedAtUtc` |
| `TransactionNormalized` | Integration Event | `TransactionAggregator` | `TransacaoId`, `ContaId`, `Valor`, `HashUnico`, `NormalizedAt` |
| `OrcamentoExcedidoEvent` | Domain Event | `TransactionAggregator` | `OrcamentoId`, `UsuarioId`, `CategoriaId`, `Limite`, `ValorAtual` |

---

## 4. Key State Transitions

### 4.1 Consent State Transition (Open Finance)
```
[ AwaitingAuthorisation ] ──(User Grants in Bank App)──> [ Authorised ]
          │                                                    │
          ├──(User Rejects)──> [ Rejected ]                   ├──(Token Expired / 1 Year)──> [ Expired ]
          │                                                    │
          └──(Timeout / Abandoned)──> [ Expired ]             └──(User Revokes in FinanceHub or Bank)──> [ Revoked ]
```

### 4.2 Account Sync & Transaction Ingestion Lifecycle
1. `PluggyIntegration` verifies active connection status with Meu.Pluggy API.
2. If session token expires or requires renewal, `PluggyIntegration` triggers proactive token authentication.
3. `PluggyIntegration` fetches checking account movements and credit card invoice items from Brazilian banks (Itaú, Inter, Mercado Pago).
4. `PluggyIntegration` publishes `TransactionIngested` or `InvoiceItemIngested` integration events via MassTransit/RabbitMQ using the Transactional Outbox Pattern.
5. `TransactionAggregator` consumes `TransactionIngested` / `InvoiceItemIngested`, calculates `HashUnico` (SHA-256) for deduplication, persists canonical `Transacao`, and emits `TransactionNormalized`.


