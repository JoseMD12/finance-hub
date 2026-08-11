# Open Finance Brasil API Specification & Integration Standard

This document details the technical specification for integrating **FinanceHub** with the **Open Finance Brasil** ecosystem in compliance with Central Bank of Brazil (BACEN) standards and FAPI 1.0 Advanced / FAPI 2.0 Security Profiles.

---

## 1. Security Architecture & Transport Layer

### 1.1 Mutual TLS (mTLS) Requirements
- **Certificate Standard**: Must use ICP-Brasil issued X.509 V3 digital certificates (e-CNPJ or Open Finance Specific Software Certificates issued by accredited CAs such as Certisign, Serasa, or Soluti) managed via `FinanceHub.Shared.Certificates`.
- **TLS Protocol**: TLS 1.2 minimum, TLS 1.3 recommended.
- **Allowed Cipher Suites**:
  - `TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256`
  - `TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384`
  - `TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256`
  - `TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384`
- **mTLS HTTP Headers**:
  - `x-fapi-financial-id`: Identifier of the financial institution target.
  - `x-fapi-customer-ip-address`: IP address of the end user initiating the request.
  - `x-fapi-interaction-id`: UUIDv4 tracking header for audit trails (must be logged on every request/response alongside OpenTelemetry `traceparent`).

### 1.2 OAuth2 & JWT Client Authentication (`private_key_jwt`)
`FinanceHub.AuthConsent` authenticates against Open Finance authorization servers using signed JWT assertions (`private_key_jwt`):
- **Client Assertion Type**: `urn:ietf:params:oauth:client-assertion-type:jwt-bearer`
- **Algorithm**: `PS256` (RSASSA-PSS using SHA-256 and MGF1 with SHA-256) or `ES256`.
- **Assertion Header**:
  ```json
  {
    "alg": "PS256",
    "typ": "JWT",
    "kid": "financehub-key-2026-01"
  }
  ```
- **Assertion Claims**:
  ```json
  {
    "iss": "https://auth.financehub.com.br",
    "sub": "client_id_registered_in_directory",
    "aud": "https://auth.bank.com.br/oauth/token",
    "jti": "d3b07384-d113-40a6-a1e3-1635441692c6",
    "exp": 1770678000,
    "iat": 1770677700
  }
  ```

---

## 2. Consents API Breakdown (`FinanceHub.AuthConsent`)

### 2.1 Endpoint Specification
- `POST /open-banking/consents/v2/consents`: Request a new consent identifier.
- `GET /open-banking/consents/v2/consents/{consentId}`: Retrieve consent state.
- `DELETE /open-banking/consents/v2/consents/{consentId}`: Revoke active consent.

### 2.2 Request Payload (`POST /consents`)
```json
{
  "data": {
    "permissions": [
      "ACCOUNTS_READ",
      "ACCOUNTS_BALANCES_READ",
      "ACCOUNTS_TRANSACTIONS_READ",
      "RESOURCES_READ"
    ],
    "expirationDateTime": "2027-08-10T23:59:59Z",
    "transactionFromDateTime": "2025-08-10T00:00:00Z",
    "transactionToDateTime": "2026-08-10T23:59:59Z"
  }
}
```

### 2.3 Consent Lifecycle & Status Codes
- `AWAITING_AUTHORISATION`: Initial state upon creation. User redirected to bank web/app via PAR (Pushed Authorization Request).
- `AUTHORISED`: User completed authentication & MFA at bank app. Valid for access token exchange.
- `REJECTED`: User denied consent or MFA failed.
- `REVOKED`: User or FinanceHub explicitly cancelled consent.
- `EXPIRED`: Consent exceeded `expirationDateTime` (max 1 year in Open Finance Brasil).

---

## 3. Accounts API Breakdown (Bank Integration Services)

### 3.1 List Accounts
- **Endpoint**: `GET /open-banking/accounts/v2/accounts`
- **Headers**: `Authorization: Bearer <access_token>`, `x-fapi-interaction-id: <uuid>`
- **Response Structure**:
```json
{
  "data": [
    {
      "brandName": "Banco Exemplo S.A.",
      "companyCnpj": "00000000000191",
      "type": "CONTA_CORRENTE",
      "compeCode": "001",
      "branchCode": "0001",
      "number": "12345678",
      "checkDigit": "9",
      "accountId": "92788437-3422-446a-85d1-9321e0638515"
    }
  ],
  "links": {
    "self": "https://api.banco.com.br/open-banking/accounts/v2/accounts?page=1",
    "next": null
  },
  "meta": {
    "totalRecords": 1,
    "totalPages": 1
  }
}
```

### 3.2 Read Account Balances
- **Endpoint**: `GET /open-banking/accounts/v2/accounts/{accountId}/balances`
- **Response Payload**:
```json
{
  "data": {
    "availableAmount": {
      "amount": "12500.50",
      "currency": "BRL"
    },
    "blockedAmount": {
      "amount": "0.00",
      "currency": "BRL"
    },
    "automaticallyInvestedAmount": {
      "amount": "5000.00",
      "currency": "BRL"
    }
  }
}
```

---

## 4. Transactions API Breakdown (Bank Integration Services)

### 4.1 Read Account Transactions
- **Endpoint**: `GET /open-banking/accounts/v2/accounts/{accountId}/transactions`
- **Query Parameters**:
  - `fromBookingDate` (Required, YYYY-MM-DD): Start date filter.
  - `toBookingDate` (Required, YYYY-MM-DD): End date filter (max range: 7 days per call or 1 year depending on institution).
  - `page` (Optional, Default 1): Page number.
  - `pageSize` (Optional, Default 25, Max 1000): Items per page.
- **Response Structure**:
```json
{
  "data": [
    {
      "transactionId": "TX-99882233-001",
      "completedAuthorisedPaymentType": "PIX",
      "creditDebitType": "DEBITO",
      "transactionName": "PIX ENVIADO MERCADO LIVRE",
      "type": "PIX",
      "amount": {
        "amount": "149.90",
        "currency": "BRL"
      },
      "transactionDate": "2026-08-10",
      "partieCnpjCpf": "10573521000191",
      "partiePersonType": "JURIDICA",
      "partieCompeCode": "341"
    }
  ],
  "links": {
    "self": "https://api.banco.com.br/open-banking/accounts/v2/accounts/92788437-3422-446a-85d1-9321e0638515/transactions?page=1",
    "next": "https://api.banco.com.br/open-banking/accounts/v2/accounts/92788437-3422-446a-85d1-9321e0638515/transactions?page=2"
  },
  "meta": {
    "totalRecords": 142,
    "totalPages": 6
  }
}
```

---

## 5. Error Handling & Standard Error Payload (RFC 7807)

Open Finance API errors conform to RFC 7807 `application/problem+json`:

```json
{
  "errors": [
    {
      "code": "STATUS_CONSENTIMENTO_INVALIDO",
      "title": "Consentimento Revogado ou Expirado",
      "detail": "O consentimento informado (urn:banco:consent:9921) encontra-se no status REVOKED.",
      "requestDateTime": "2026-08-10T21:06:08Z"
    }
  ]
}
```

### Standard HTTP Status Codes

| HTTP Status | Meaning | Action in FinanceHub |
|---|---|---|
| `200 OK` / `201 Created` | Success | Parse data and publish `TransactionIngested` event. |
| `400 Bad Request` | Invalid payload or params | Log error and throw domain validation exception. |
| `401 Unauthorized` | Invalid/expired access token | `FinanceHub.AuthConsent` triggers token refresh using `refresh_token`; retry once. |
| `403 Forbidden` | Consent revoked / scope mismatch | Mark `ConexaoOpenFinance` as `Revoked` or `Expired`. Notify user. |
| `429 Too Many Requests` | Rate limit exceeded | Backoff exponentially based on `Retry-After` header. |
| `500 / 502 / 503` | Bank server error | Retry with jitter (up to 3 retries). Mark connection degraded if persistent. |

