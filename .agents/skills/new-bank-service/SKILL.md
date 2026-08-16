---
name: new-bank-service
description: Comprehensive enterprise guide and specification framework for adding and integrating a new bank or financial institution service (e.g., Itaú, Mercado Pago, Banco Inter, Nubank) in FinanceHub (.NET 10 / C# 13), covering Clean Architecture, cross-service harmony, IBankConnector, OAuth2/mTLS, Polly resilience, MassTransit Transactional Outbox, LGPD PII sanitization, and TDD Red-Green-Refactor workflows.
---

# New Bank Service Integration Framework — FinanceHub (.NET 10 / C# 13)

This skill provides an end-to-end, generalized specification and implementation guide for adding a new bank or financial institution integration service to **FinanceHub**. It guarantees strict adherence to Clean Architecture, DDD, Open Finance Brasil standards, distributed cross-service compatibility, and enterprise security guardrails.

---

## ⚡ Trigger / Slash Command
```bash
/new-bank-service <BankName>
```

---

## 🏛️ 1. Architecture & Cross-Service Topology

Each bank integration operates as an autonomous, isolated microservice (`FinanceHub.<BankName>Integration`) communicating asynchronously via MassTransit over RabbitMQ and PostgreSQL.

```text
[ Frontend Web / Mobile ]
           │ (Bearer JWT)
           ▼
[ FinanceHub.ApiGateway (BFF) ]
   ├── (Consent Authorize/Revoke) ──────────────► [ FinanceHub.AuthConsent ]
   └── (Trigger On-Demand Sync: 202 Accepted) ──► [ FinanceHub.<BankName>Integration ]
                                                              │
                                     (Fetches Token) ◄────────┘
                                     (KMS Decrypted)
                                                              │
                                     (mTLS / OAuth2) ────────► [ External Bank API / Open Finance ]
                                                              │
                               (Publishes: TransactionIngested)
                               (MassTransit Outbox + Postgres)
                                                              ▼
                                                 [ RabbitMQ Topic Exchange ]
                                                              │
                                                              ▼
                                               [ FinanceHub.TransactionAggregator ]
                                                              │
                                            (Deduplication SHA-256 Hash Check)
                                            (Canonical Transaction & Account Balance)
                                                              ▼
                                               [ PostgreSQL Canonical Ledger ]
```

---

## 🔑 2. Cross-Service Harmonization Rules (Zero Mismatch Policy)

When planning or scaffolding a new bank service, all 5 system layers must be synchronized:

### 2.1 Bank Identifier String (Strict Lowercase)
- **Rule**: The bank identifier must be centralized in `FinanceHub.AuthConsent.Domain.Constants.BankIdentifiers` and mirrored in `FinanceHub.<BankName>Integration.Domain.Constants.<BankName>Constants.BankIdentifier` as an identical lowercase string (e.g., `"itau"`, `"mercadopago"`, `"inter"`, `"nubank"`).
- **Rationale**: Deterministic SHA-256 deduplication in `TransactionAggregator` relies on `TransactionHash.ComputeHash(institutionId, ...)` — any casing mismatch creates silent ledger duplicates.

### 2.2 Canonical Event Contract (`TransactionIngested`)
All bank connectors must emit the canonical `TransactionIngested` event implementing `IFinanceHubEvent` (`FinanceHub.Shared.Messaging`):

```csharp
namespace FinanceHub.Shared.Messaging.Events;

public record TransactionIngested(
    Guid IngestionId,
    string UserId,          // Internal FinanceHub authenticated User ID
    string Source,          // Bank identifier (e.g., "mercadopago", "itau")
    string AccountId,       // Bank Account ID / Collector ID / IBAN
    string? BankTransactionId, // External Unique Transaction/Payment ID
    decimal Amount,         // Signed decimal (+ for incoming/credits, - for outgoing/debits)
    DateTime TransactionDate,// UTC Timestamp of payment/clearing
    string Description,     // Cleaned original narrative / counterparty
    string Currency,        // ISO 4217 Currency Code ("BRL")
    string? RawPayloadJson, // LGPD-sanitized JSON (all PII redacted)
    DateTime OccurredAtUtc  // Timestamp when event was ingested
) : IFinanceHubEvent;
```

### 2.3 Aggregator Consumer Registration
Ensure `TransactionAggregator` registers `IConsumer<TransactionIngested>` (`TransactionIngestedConsumer`) to map the message to `IngestTransactionCommand` and update `AccountBalance`.

### 2.4 BFF Gateway Routing (`FinanceHub.ApiGateway`)
1. Register downstream URL in `GatewayConstants.Downstream.<BankName>BaseUrlEnvVar`.
2. Implement `I<BankName>ServiceClient` with `.AddStandardResilienceHandler()`.
3. Map sync endpoint: `POST /api/v1/gateway/<bank>/sync`.

---

## 🛡️ 3. Security, LGPD & Financial Integrity Guardrails

### 3.1 LGPD & PII Sanitization (Mandatory Masking)
- **Zero Raw PII on Message Broker**: Bank APIs return sensitive counterparty data (CPF/CNPJ, full names, email addresses, phone numbers, raw card numbers).
- **Sanitization Pipeline**: All raw bank responses must pass through a sanitization filter before storing in `RawPayloadJson` or database logs:
  - Mask CPF: `***.***.123-**`
  - Mask CNPJ: `**.***.***/0001-**`
  - Redact Cardholder PAN & CVV completely.

### 3.2 Financial Precision & Fee Separation
- **Signed Decimal Amounts**:
  - **Credit (+)**: Incoming Pix, salary, deposits, received transfers.
  - **Debit (-)**: Outgoing Pix, purchases, bank fees, withdrawals.
- **Gross vs Fee Separation**: When the bank API returns fees (e.g. gateway processing fees, interchange), emit the **gross transaction** as credit/debit AND emit a separate linked **fee transaction** as debit. Never silently swallow fees in net amounts.

### 3.3 Dynamic Cursor & Status Lifecycle Ingestion
- **Cursor Field**: Always filter date ranges by `date_last_updated` / `bookingDateTime` rather than `date_created`.
- **Status Lifecycle**: Explicitly map status changes (`approved`, `refunded`, `charged_back`, `cancelled`). Reversals/refunds must be emitted with compensatory signed amounts.

---

## 📦 4. Standard Adapter Structure & `IBankConnector`

Scaffold the following folder structure inside `src/Services/FinanceHub.<BankName>Integration/`:

```text
FinanceHub.<BankName>Integration/
├── FinanceHub.<BankName>Integration.Domain/
│   ├── Constants/
│   │   └── <BankName>Constants.cs
│   ├── Entities/
│   │   └── <BankName>SyncState.cs
│   └── Exceptions/
│       ├── NullOrEmpty<BankName>CredentialsDomainException.cs
│       ├── <BankName>UnauthorizedDomainException.cs
│       ├── <BankName>AccountNotFoundDomainException.cs
│       ├── <BankName>InvalidConsentStateDomainException.cs
│       ├── <BankName>RateLimitExceededDomainException.cs
│       └── <BankName>ApiCommunicationDomainException.cs
├── FinanceHub.<BankName>Integration.Application/
│   ├── Commands/SyncTransactions/
│   │   ├── ISync<BankName>TransactionsCommandHandler.cs
│   │   ├── Sync<BankName>TransactionsCommand.cs
│   │   └── Sync<BankName>TransactionsCommandHandler.cs
│   └── DependencyInjection.cs
├── FinanceHub.<BankName>Integration.Infrastructure/
│   ├── Configuration/
│   │   └── <BankName>Options.cs
│   ├── Security/
│   │   └── <BankName>AuthHandler.cs
│   ├── Connectors/
│   │   └── <BankName>Connector.cs
│   ├── Mapping/
│   │   └── <BankName>MappingProfile.cs
│   ├── Persistence/
│   │   ├── <BankName>DbContext.cs
│   │   └── Configurations/<BankName>SyncStateConfiguration.cs
│   └── DependencyInjection.cs
└── FinanceHub.<BankName>Integration.Api/
    ├── Endpoints/
    │   └── SyncEndpoints.cs
    ├── Program.cs
    └── DependencyInjection.cs
```

### 4.1 Canonical `IBankConnector` Interface
```csharp
namespace FinanceHub.Shared.Connectors;

public interface IBankConnector
{
    string BankIdentifier { get; }
    
    Task<AuthTokenResponse> AuthenticateAsync(
        BankCredentials credentials, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankAccountDto>> GetAccountsAsync(
        AuthTokenResponse token, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsAsync(
        AuthTokenResponse token, 
        string accountId, 
        DateTimeOffset from, 
        DateTimeOffset to, 
        CancellationToken cancellationToken = default);

    Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

---

## ⚙️ 5. Resilience & MassTransit Transactional Outbox Setup

### 5.1 Polly v8+ Resilience Pipeline
Configure HttpClient resilience in Infrastructure `DependencyInjection.cs`:
```csharp
services.AddHttpClient<<BankName>Connector>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddStandardResilienceHandler(resilience =>
{
    resilience.Retry.MaxRetryAttempts = <BankName>Constants.MaxRetryAttempts;
    resilience.Retry.Delay = TimeSpan.FromSeconds(2);
    resilience.Retry.BackoffType = DelayBackoffType.Exponential;
    resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    resilience.CircuitBreaker.FailureRatio = 0.5;
});
```

### 5.2 Transactional Outbox Atomic Boundary
The sync command handler MUST persist the sync cursor (`<BankName>SyncState`) and publish `TransactionIngested` within the same EF Core database transaction:

```csharp
public async Task<SyncResultDto> Handle(Sync<BankName>TransactionsCommand command, CancellationToken ct)
{
    // 1. Fetch transactions from Bank Connector
    var transactions = await _connector.GetTransactionsAsync(...);

    // 2. Publish to MassTransit Outbox
    foreach (var tx in transactions)
    {
        await _publishEndpoint.Publish(tx.ToTransactionIngested(command.UserId), ct);
    }

    // 3. Update Sync State Cursor
    syncState.UpdateCursor(latestTransactionDateUtc, _timeProvider);
    _dbContext.SyncStates.Update(syncState);

    // 4. Atomic Commit (Cursor + Outbox messages)
    await _dbContext.SaveChangesAsync(ct);

    return new SyncResultDto(syncState.Id, transactions.Count, syncState.LastSyncCursorUtc);
}
```

---

## 🚦 6. Mandatory TDD Red -> Green -> Refactor Workflow

Feature delivery must strictly follow the TDD 3-stage cycle:

### 🔴 Stage 1: Red (Write Failing Unit & Integration Tests First)
1. `<BankName>AuthHandlerTests`: Token caching, bearer header attachment, handling 401 token refresh.
2. `<BankName>ConnectorTests`: Pagination offset/limit traversal, HTTP 429 backoff handling, and PII masking.
3. `<BankName>MappingProfileTests`: Signed decimal validation, fee separation, and currency normalization.
4. `Sync<BankName>TransactionsCommandHandlerTests`: Atomic cursor commit and MassTransit Outbox publication.
5. `TransactionIngestedConsumerTests`: Consumer execution and ledger deduplication in `TransactionAggregator`.

### 🟢 Stage 2: Green (Write Minimal Implementation Code)
- Implement domain entities, exceptions, constants, connector, handler, and endpoints until all tests pass.

### 🟡 Stage 3: Refactor (Clean Code & Architecture Polish)
- Eliminate memory allocations on JSON serialization (use `System.Text.Json` source generators).
- Verify interface/implementation separation across dedicated individual `.cs` files.
- Verify zero magic strings/numbers and assert minimum 80% test coverage per layer.

---

## 📋 7. Feature Planning & Specification Checklist

When using `/new-bank-service <BankName>` to plan a new connector, verify:
- [ ] Bank identifier constant is defined in lowercase in both `BankIdentifiers.cs` and `<BankName>Constants.cs`.
- [ ] Authentication mechanism chosen (Open Finance FAPI mTLS vs Proprietary OAuth 2.0).
- [ ] LGPD PII masking rules explicitly documented for all API payload fields.
- [ ] Gross amounts and fee deduction mappings defined.
- [ ] Date filtering uses `date_last_updated` cursor to track reversals and status changes.
- [ ] RFC 7807 typed domain exceptions mapped for all error conditions (400, 401, 404, 409, 429, 502).
- [ ] Asynchronous `202 Accepted` endpoint specified for on-demand dashboard sync.
- [ ] MassTransit Outbox and `TransactionIngestedConsumer` specified end-to-end.
- [ ] Concrete TDD test plan documented with Red/Green/Refactor test scenarios upfront.
