# Feature Specification: Mercado Pago Integration Adapter

## 1. Overview & Business Goal
Integrate FinanceHub with **Mercado Pago** to automatically ingest, sanitize, and normalize financial transactions (Pix, incoming/outgoing payments, marketplace fee breakdowns) and account balance data into `FinanceHub.TransactionAggregator` via asynchronous integration events (`TransactionIngested`) dispatched using MassTransit Transactional Outbox over RabbitMQ and PostgreSQL.

---

## 2. Cross-Service & Domain Model Harmony Matrix (`src/`)

| Concern | Source of Truth (`src/`) | Mercado Pago Integration Alignment |
| :--- | :--- | :--- |
| **Bank Identifier** | `BankIdentifiers.MercadoPago = "mercadopago"` | `MercadoPagoConstants.BankIdentifier = "mercadopago"` (all lowercase) |
| **Event Contract** | `FinanceHub.Shared.Messaging.Events.TransactionIngested` | Extended to include `string UserId` and implement `IFinanceHubEvent` |
| **Token Retrieval** | `FinanceHub.AuthConsent` (`BankConsent` aggregate) | Internal API: `GET /api/v1/consents/internal/{userId}/mercadopago/token` |
| **BFF Gateway Route** | `FinanceHub.ApiGateway` (`GatewayConstants.Downstream`) | `POST /api/v1/gateway/mercadopago/sync` -> `MercadoPagoIntegration` |
| **Ledger Ingestion** | `FinanceHub.TransactionAggregator` | `TransactionIngestedConsumer` dispatches `IngestTransactionCommand` |
| **Deduplication Hash** | `TransactionHash.ComputeHash` | Deterministic SHA-256: `$"mercadopago:{accountId}:{bankTxId}:{amount:F2}:{dateUtc:O}"` |

---

## 3. External API Contracts & Integration Strategy

### 3.1 External API Endpoints (Mercado Pago REST)
- **Base URL**: `https://api.mercadopago.com`
- **Authentication**: Dynamic OAuth 2.0 Bearer tokens managed per user via `FinanceHub.AuthConsent` (`MercadoPagoOAuthStrategy.cs`). Zero static user access tokens in `.env`.
- **User Account**: `GET /users/me` -> Extracts `user_id` (Collector ID), account nickname, and currency settings.
- **Transactions & Statements**: `GET /v1/payments/search?sort=date_last_updated&criteria=desc&range=date_last_updated&begin_date={from}&end_date={to}&offset={offset}&limit={limit}`
  - Cursor tracking strictly uses `date_last_updated` (not `date_created`) to capture status transitions (`approved` -> `refunded` / `charged_back`).
- **Health Check Probe**: `GET /v1/payment_methods` or `GET /users/me`

### 3.2 Canonical Adapter Abstraction (`IBankConnector`)
`MercadoPagoConnector` implements standard `IBankConnector`:
```csharp
namespace FinanceHub.Shared.Connectors;

public interface IBankConnector
{
    string BankIdentifier { get; } // "mercadopago"
    Task<AuthTokenResponse> AuthenticateAsync(BankCredentials credentials, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BankAccountDto>> GetAccountsAsync(AuthTokenResponse token, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsAsync(AuthTokenResponse token, string accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

### 3.3 Event Contract & Ingestion Payload (`TransactionIngested`)
Harmonized contract implementing `IFinanceHubEvent`:
```csharp
namespace FinanceHub.Shared.Messaging.Events;

public record TransactionIngested(
    Guid IngestionId,
    string UserId,
    string Source,          // "mercadopago" (BankIdentifiers.MercadoPago)
    string AccountId,       // Collector ID / Account Number
    string? BankTransactionId,
    decimal Amount,         // Signed decimal (+ for incoming/credits, - for outgoing/debits)
    DateTime TransactionDate,
    string Description,
    string Currency,        // "BRL"
    string? RawPayloadJson, // LGPD-sanitized JSON (redacted PII)
    DateTime OccurredAtUtc
) : IFinanceHubEvent;
```

---

## 4. Resilience, Rate Limiting & Outbox Architecture

### 4.1 Polly v8+ Resilience Pipeline
Configure `StandardResilienceHandler` & Rate Limiting for `HttpClient`:
- **Rate Limiter**: Token bucket / sliding window capped at 120 req/min (`MercadoPagoConstants.DefaultRateLimitPerMinute`).
- **Retry Policy with `Retry-After`**: 3 attempts with exponential backoff + jitter; respects HTTP 429 `Retry-After` header.
- **Circuit Breaker**: 50% failure ratio over 30-second sampling window with 15-second break duration.
- **Timeouts**: 10s per request attempt, 30s total cumulative timeout.

### 4.2 Asynchronous On-Demand UX & State Persistence
- **API Endpoint**: `POST /api/v1/mercadopago/sync`
  - Returns `202 Accepted` with `SyncJobId` and initial status `IN_PROGRESS` (avoids gateway timeouts).
  - Sync state persisted in `MercadoPagoSyncState` aggregate (`AccountId`, `LastSyncCursorUtc`, `LastExecutionUtc`, `Status`).
- **MassTransit Transactional Outbox**:
  - `MercadoPagoDbContext` stores both `MercadoPagoSyncState` updates and Outbox messages atomically in a single PostgreSQL transaction (`AddEntityFrameworkOutbox<MercadoPagoDbContext>`).

---

## 5. RFC 7807 Domain Exceptions Mapping

| Exceção | Condição de Disparo | Status HTTP | ErrorCode |
| :--- | :--- | :---: | :--- |
| `NullOrEmptyMercadoPagoCredentialsDomainException` | ClientId ou ClientSecret ausentes | 400 | `INVALID_MERCADO_PAGO_CREDENTIALS` |
| `MercadoPagoUnauthorizedDomainException` | Token expirado, revogado ou inválido (401 da API MP) | 401 | `MERCADO_PAGO_UNAUTHORIZED` |
| `MercadoPagoAccountNotFoundDomainException` | Conta de usuário do Mercado Pago não localizada | 404 | `MERCADO_PAGO_ACCOUNT_NOT_FOUND` |
| `MercadoPagoInvalidConsentStateDomainException` | Consentimento não está em estado `Authorised` | 409 | `MERCADO_PAGO_CONSENT_INVALID_STATE` |
| `MercadoPagoRateLimitExceededDomainException` | HTTP 429 recebido após esgotar retentativas | 429 | `MERCADO_PAGO_RATE_LIMIT_EXCEEDED` |
| `MercadoPagoApiCommunicationDomainException` | Falha HTTP 5xx ou gateway timeout na API MP | 502 | `MERCADO_PAGO_GATEWAY_ERROR` |

---

## 6. Zero Magic Strings & Numbers (`MercadoPagoConstants`)

```csharp
namespace FinanceHub.MercadoPagoIntegration.Domain.Constants;

public static class MercadoPagoConstants
{
    public const string BankIdentifier = "mercadopago"; // Harmonized with BankIdentifiers.MercadoPago
    public const string DefaultCurrency = "BRL";
    public const string BaseUrl = "https://api.mercadopago.com";
    public const string PaymentsSearchEndpoint = "v1/payments/search";
    public const string UsersMeEndpoint = "users/me";
    public const string PaymentMethodsEndpoint = "v1/payment_methods";
    public const int DefaultRateLimitPerMinute = 120;
    public const int DefaultPageSize = 50;
    public const int MaxRetryAttempts = 3;
}
```

---

## 7. TDD Implementation Plan (Red -> Green -> Refactor)

### 🔴 Red Phase (Failing Test Scenarios)
1. **`MercadoPagoAuthHandlerTests`**:
   - `Should_AttachBearerToken_When_ValidTokenCached`
   - `Should_ThrowMercadoPagoUnauthorizedDomainException_When_ApiReturns401`
2. **`MercadoPagoConnectorTests`**:
   - `Should_PaginateAndAccumulatePayments_When_MultiplePagesReturned`
   - `Should_ThrowRateLimitExceeded_When_Http429_PersistsAfter3Retries`
   - `Should_SanitizePiiFromPayerData_InRawPayloadJson`
3. **`MercadoPagoMappingProfileTests`**:
   - `Should_MapSignedAmounts_AndSeparateFees_IntoIntegrationEvents`
   - `Should_HandleRefundsAndChargebacks_WithCorrectNegativeDeltas`
4. **`SyncMercadoPagoTransactionsCommandHandlerTests`**:
   - `Should_CommitSyncCursor_And_PublishOutboxEvents_InSingleDbTransaction`
   - `Should_ThrowMercadoPagoInvalidConsentStateDomainException_When_ConsentNotAuthorised`
5. **`TransactionIngestedConsumerTests` (`TransactionAggregator`)**:
   - `Should_ConsumeTransactionIngested_And_ExecuteIngestTransactionCommand`

### 🟢 Green Phase (Minimal Production Code)
- Implement `MercadoPagoOptions`, `MercadoPagoConstants`, and custom `DomainException` classes.
- Implement `MercadoPagoAuthHandler`, `MercadoPagoConnector : IBankConnector`, and `MercadoPagoMappingProfile`.
- Implement `ISyncMercadoPagoTransactionsCommandHandler` and its handler with EF Core Outbox integration.
- Implement `TransactionIngestedConsumer` in `TransactionAggregator`.
- Configure `IMercadoPagoServiceClient` and routes in `ApiGateway`.

### 🟡 Refactor Phase (Optimization & Clean Architecture)
- Optimize string allocations and JSON stream deserialization via `System.Text.Json` source generators.
- Verify separation of interfaces and implementations across dedicated `.cs` files.
- Enforce full code coverage (>80%) and verify OpenTelemetry `traceparent` propagation.

---

## 8. Required Environment Variables for Real Testing
To test and execute against real Mercado Pago APIs, the following credentials are provided via `.env`:
1. `MERCADO_PAGO_CLIENT_ID`: Application Client ID from Mercado Pago Developers Portal.
2. `MERCADO_PAGO_CLIENT_SECRET`: Application Client Secret.
3. `MERCADO_PAGO_REDIRECT_URI`: OAuth callback URI.
4. `MERCADO_PAGO_BASE_URL`: `https://api.mercadopago.com`.
