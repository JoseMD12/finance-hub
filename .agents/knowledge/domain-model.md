# FinanceHub - Domain Model Specification (.NET 10 Microservices / DDD)

This document defines the core Domain Driven Design (DDD) model for **FinanceHub**, built on **.NET 10** and **C# 13**. It outlines Aggregates, Entities, Value Objects, Domain/Integration Events, and State Transitions, mapped cleanly across FinanceHub microservices boundaries.

---

## 🏛️ Microservice Domain Boundaries & Database Isolation

FinanceHub enforces **Database-per-Service** isolation. Each aggregate resides strictly within its owning microservice:

- **`FinanceHub.AuthConsent`**: Manages `ConexaoOpenFinance`, consent lifecycle, OAuth2 tokens (`BankCredentials`), and FAPI security parameters.
- **`FinanceHub.TransactionAggregator`**: Manages core financial domain models: `ContaFinanceira`, `Instituicao`, `Transacao`, `Categoria`, and `Orcamento`.
- **`FinanceHub.ItauIntegration` / `FinanceHub.MercadoPagoIntegration` / `FinanceHub.InterIntegration`**: Stateless bank integration services. Translate external financial payloads into `TransactionIngested` integration events via MassTransit/RabbitMQ + Transactional Outbox Pattern.

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

### 1.6 `ConexaoOpenFinance` (Aggregate Root — `AuthConsent`)
Manages Open Finance consent tokens, mTLS parameters, and sync status for a specific bank link.
- **Properties**:
  - `Id`: `ConexaoId`
  - `UsuarioId`: `UsuarioId`
  - `InstituicaoId`: `InstituicaoId`
  - `ConsentId`: `ConsentId` (Value Object representing Open Finance Consent UUID)
  - `StatusConsentimento`: `StatusConsentimentoEnum` (`AwaitingAuthorisation`, `Authorised`, `Rejected`, `Revoked`, `Expired`)
  - `DataExpiracaoConsentimento`: `DateTimeOffset`
  - `UltimaSincronizacao`: `DateTimeOffset?`
  - `Credenciais`: `BankCredentials` (Encrypted token store Value Object)

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
| `TransactionIngested` | Integration Event | Bank Integration Services | `ExternalAccountId`, `BankCode`, `Amount`, `Description`, `RawPayload`, `IngestedAt` |
| `TransactionNormalized` | Integration Event | `TransactionAggregator` | `TransacaoId`, `ContaId`, `Valor`, `HashUnico`, `NormalizedAt` |
| `OrcamentoExcedidoEvent` | Domain Event | `TransactionAggregator` | `OrcamentoId`, `UsuarioId`, `CategoriaId`, `Limite`, `ValorAtual` |
| `ConexaoOpenFinanceExpiradaEvent` | Integration Event | `AuthConsent` | `ConexaoId`, `UsuarioId`, `ConsentId`, `DataExpiracao` |

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
1. `AuthConsent` verifies active `Authorised` status for `ConexaoOpenFinance`.
2. If `ExpiresAt` < `DateTimeOffset.UtcNow`, `AuthConsent` triggers token refresh using `RefreshTokenEncrypted`.
3. Bank Integration Service (`ItauIntegration`, `MercadoPagoIntegration`, etc.) fetches bank statement items using mTLS client certificates (`FinanceHub.Shared.Certificates`).
4. Bank Integration publishes `TransactionIngested` integration event via MassTransit/RabbitMQ using the Transactional Outbox Pattern.
5. `TransactionAggregator` consumes `TransactionIngested`, calculates `HashUnico` for deduplication, persists canonical `Transacao`, and emits `TransactionNormalized`.

