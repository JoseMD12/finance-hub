# Technical Specification: Direct Bank Connectors (Itaú, Banco Inter, Mercado Pago)

> **Status**: `Superseded by Unified FinanceHub.PluggyIntegration Engine`  
> **Note**: Direct individual connectors (Itaú, Inter, Mercado Pago) have been unified into `FinanceHub.PluggyIntegration` for seamless Open Finance account sync.

---

## 1. Itaú Direct API Connector (`FinanceHub.ItauIntegration`)

### 1.1 Authentication & Security
- **OAuth2 Flow**: Client Credentials Flow with mTLS managed via `FinanceHub.Shared.Certificates` (`private_key_jwt` or mTLS client cert + `client_id` / `client_secret`).
- **Base URL**: `https://api.itau.com.br` (Production) / `https://sandbox.api.itau.com.br` (Sandbox)
- **Token Endpoint**: `POST /oauth/token`
- **Grant Type**: `client_credentials`
- **Scope**: `read_account_statements read_credit_card_statements`

### 1.2 Endpoints & Pagination
- **Get Account Statement**:
  - `GET /api/v1/conta-corrente/extrato`
  - **Query Parameters**:
    - `dataInicio` (YYYY-MM-DD)
    - `dataFim` (YYYY-MM-DD)
    - `pagina` (int, default: 1)
    - `tamanhoPagina` (int, default: 50, max: 200)
- **Get Credit Card Transactions**:
  - `GET /api/v1/cartao-credito/faturas/transacoes`
  - **Query Parameters**:
    - `cartaoId` (string)
    - `mesFatura` (YYYY-MM)

### 1.3 Rate Limits & Event Emission
- **Rate Limit**: 100 requests / minute per client application.
- **Header**: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`.
- **429 Response handling**: Wait for `X-RateLimit-Reset` seconds before retrying.
- **Event Outbox**: Ingested transactions are published as `TransactionIngested` events via MassTransit/RabbitMQ using the Transactional Outbox Pattern.

---

## 2. Banco Inter API Connector (`FinanceHub.InterIntegration`)

### 2.1 Authentication & Security
- **Authentication**: Mutual TLS (mTLS) with client certificate (`.crt` and `.key` issued in Banco Inter Internet Banking) + OAuth2 Client Credentials.
- **Base URL**: `https://cdpj.banking.bancointer.com.br`
- **Token Endpoint**: `POST /oauth/v2/token`
- **Headers**:
  ```http
  Content-Type: application/x-www-form-urlencoded
  grant_type=client_credentials&client_id=<CLIENT_ID>&client_secret=<CLIENT_SECRET>&scope=extrato.read cartao.read
  ```

### 2.2 Endpoints & Pagination
- **Get Account Statement (Extrato)**:
  - `GET /banking/v2/extrato`
  - **Query Parameters**:
    - `dataInicio` (YYYY-MM-DD)
    - `dataFim` (YYYY-MM-DD)
    - `pagina` (int, default: 0)
    - `tamanhoPagina` (int, default: 100)
- **Response Format**:
  ```json
  {
    "transacoes": [
      {
        "dataEntrada": "2026-08-10",
        "tipoTransacao": "PIX",
        "tipoLancamento": "CREDITO",
        "valor": "350.00",
        "titulo": "Pix Recebido",
        "descricao": "Transferencia recebida via Pix"
      }
    ],
    "totalPaginas": 1,
    "totalElementos": 1,
    "ultimaPagina": true
  }
  ```

### 2.3 Webhook Handling & Verification
- **Register Webhook**: `PUT /banking/v2/extrato/webhook`
  - Payload: `{ "webhookUrl": "https://api.financehub.com.br/api/v1/webhooks/inter" }`
- **Webhook Payload Format**:
  ```json
  {
    "dataEnvio": "2026-08-10T21:06:08Z",
    "evento": "NOVA_TRANSACAO",
    "conta": "12345678",
    "valor": "350.00",
    "tipoLancamento": "CREDITO"
  }
  ```
- **Signature Verification**: Validates request against Banco Inter public certificate / IP whitelist + bearer signature header `x-inter-signature`.

---

## 3. Mercado Pago API Connector (`FinanceHub.MercadoPagoIntegration`)

### 3.1 Authentication & Security
- **Authentication**: OAuth2 Authorization Code Grant (for multi-tenant user account linking) or Long-lived Access Token.
- **Base URL**: `https://api.mercadopago.com`
- **Token Endpoint**: `POST /oauth/token`
- **Headers**: `Authorization: Bearer <APP_ACCESS_TOKEN>`

### 3.2 Endpoints & Pagination
- **Search Account Balance**:
  - `GET /users/me/mercadopago_account/balance`
- **Search Payments / Transactions**:
  - `GET /v1/payments/search`
  - **Query Parameters**:
    - `begin_date` (ISO-8601: `2026-08-01T00:00:00Z`)
    - `end_date` (ISO-8601: `2026-08-10T23:59:59Z`)
    - `offset` (int, default: 0)
    - `limit` (int, default: 50, max: 100)
    - `sort` (`date_created`), `criteria` (`desc`)

### 3.3 Webhook Handling & HMAC Verification
- **Webhook Notification Header**:
  - `x-signature`: Contains timestamp `ts` and HMAC `v1` hash.
  - Example: `ts=1770678368,v1=5d41402abc4b2a76b9719d911017c592...`
- **Verification Logic**:
  1. Extract `ts` and `v1` from header.
  2. Construct manifest string: `id:<data.id>;request-id:<x-request-id>;ts:<ts>;`
  3. Compute HMAC-SHA256 of manifest string using Mercado Pago Webhook Secret.
  4. Perform constant-time string comparison between computed signature and `v1`.

---

## 4. Resilience, Error Handling & Retry Strategies

### 4.1 Resiliency Strategy Matrix
- **Polly Resilience Pipeline (.NET 10)** configured per integration service:

```csharp
// FinanceHub Connector Resilience Pipeline Configuration
var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(res => res.StatusCode == HttpStatusCode.TooManyRequests || (int)res.StatusCode >= 500),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 8,
        BreakDuration = TimeSpan.FromMinutes(1)
    })
    .AddTimeout(TimeSpan.FromSeconds(10))
    .Build();
```

### 4.2 Idempotency & Deduplication Engine (`FinanceHub.TransactionAggregator`)
- FinanceHub generates a deterministic transaction fingerprint for incoming webhook or poll items:
  `SHA256(AccountId + "|" + Date.ToString("yyyyMMdd") + "|" + Amount.ToString("F2") + "|" + ExternalId)`
- Deduplication store in Redis / PostgreSQL avoids duplicate balance calculations or duplicate domain event emissions.
s.
