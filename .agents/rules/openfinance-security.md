# OpenFinance Security Standards & LGPD Compliance

This document specifies security architecture guidelines, authentication/authorization protocols, token security, and LGPD compliance for **FinanceHub** under OpenFinance Brasil specifications.

---

## 1. OAuth 2.0 / FAPI 1.0 & 2.0 Advanced Profile with PKCE
All communication with OpenFinance participant APIs must comply with Financial-grade API (FAPI) security profiles.

### Standards
* **Authorization Code Flow with PKCE**: Confidential clients must use Proof Key for Code Exchange (`code_challenge` / `code_verifier` with `S256`). Plain code challenges are strictly forbidden.
* **Mutual TLS (mTLS)**: Certificate-bound access tokens (`x5t#S256`) must be validated against client certificates during token exchange and API request execution.
* **JWE & JWS Claims**: Signed JWT requests (JAR - JWT Secured Authorization Request) and encrypted responses (JWE) must use strong cryptographic algorithms (RS256/ES256 for JWS, RSA-OAEP-256 for JWE).
* **Token Lifetime**: Access tokens must have short lifespans (max 300 seconds). Refresh tokens must be rotated upon every invocation.

---

## 2. mTLS Client Certificate Handling & ICP-Brasil Validation
OpenFinance Brasil requires mutual authentication using certificates issued by approved ICP-Brasil CAs (Certificadoras Credenciadas).

### Guidelines
* Pass client certificates via secure TLS termination or reverse proxy with forwarded `X-Client-Cert` headers validated with thumbprint verification.
* Implement strict validation of certificate chain, revocation lists (CRL / OCSP), and identity attributes (CNPJ in `OU` or `SAN`).
* Store outbound client private keys inside Hardware Security Modules (HSM) or cloud KMS/Vault key pairs. Never embed certificates or private keys in source code or `.AppSettings.json`.

---

## 3. Token & Secret Management (Vault / KMS)
Secrets, signing keys, and API tokens must be strictly isolated and encrypted.

### Guidelines
* **Vault Integration**: Fetch dynamic secrets and private keys from HashiCorp Vault, AWS KMS, or Azure Key Vault at runtime.
* **Encryption at Rest**: Encrypt OAuth access/refresh tokens stored in PostgreSQL or Redis using AES-256-GCM.
* **Key Rotation**: Implement automated rotation for dynamic client secrets and internal token-signing keys.

---

## 4. LGPD Compliance & Data Protection
FinanceHub processes sensitive personal financial data subject to Brazil's Lei Geral de Proteção de Dados (LGPD - Law 13.709/2018).

### Mandatory Controls
1. **Consent Lifecycle Management**:
   * Explicit, granular user consent must be recorded before invoking OpenFinance consent APIs.
   * Consents must have an explicit expiration date and support instant user revocation.
2. **Data Minimization & Purpose Limitation**:
   * Request only data scopes strictly required for the financial transaction or query (e.g., `accounts`, `payments`).
3. **PII Masking & Anonymization**:
   * CPF numbers must be formatted and masked in display APIs (e.g., `***.456.789-**`).
   * CNPJ numbers must obscure core operational fields (e.g., `12.***.567/0001-**`).
4. **Right to Erasure (Direito ao Esquecimento)**:
   * Provide compliant audit trails while supporting non-financial data purging upon user consent withdrawal.

---

## 5. Secure Logging & Zero-PII Policy
Exposure of PII or credentials in logs is a severe security violation.

### Prohibited Log Contents
* Raw Bearer tokens, Refresh tokens, OAuth client secrets, or private keys.
* Full Unmasked CPF, CNPJ, Bank Account Numbers, Card Numbers, CVVs, or Passwords.
* Raw payloads containing financial payloads without dynamic destructuring.

### Enforcement Mechanism
* Use structured logging (e.g., Serilog or Microsoft.Extensions.Logging) with custom **Log Sanitizing Enrichers**.
* Automatically redact fields matching sensitive pattern regexes: `cpf`, `cnpj`, `authorization`, `password`, `token`, `secret`, `accountNumber`.

```csharp
// Example Serilog PII Redaction Filter
public class PiiRedactionEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Bearer", "Password", "CPF", "CNPJ", "AccessToken", "RefreshToken", "ClientSecret"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            if (SensitiveKeys.Contains(property.Key))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, "[REDACTED]"));
            }
        }
    }
}
```

---

## 6. OWASP Top 10 API Security Protection Rules

### 🛡️ API1:2023 - Broken Object Level Authorization (BOLA / IDOR)
- **Proteção**: Todo endpoint ou handler que acesse recursos (consentimentos, contas, transações) **deve validar explicitamente se o `UserId` do JWT do chamador é idêntico ao proprietário do recurso**.
- **Regra**: Proibido buscar recursos passando apenas o ID sem filtrar pelo `UserId` do contexto autenticado.

### 🛡️ API2:2023 - Broken Authentication
- **Proteção**: Renovação automática com `refresh_token` de uso único (rotacionado a cada uso). Tokens at-rest sempre criptografados com AES-256-GCM.

### 🛡️ API3:2023 - Broken Object Property Level Authorization (Data Leaks)
- **Proteção**: Entidades de domínio nunca são serializadas diretamente na resposta HTTP. Usar exclusivamente DTOs imutáveis com mapeamento estrito.

### 🛡️ API4:2023 - Unrestricted Resource Consumption (Rate Limiting)
- **Proteção**: O `ApiGateway` aplica `AddRateLimiter` do .NET 10 limitando sincronizações manuais a no máximo 10 requisições por minuto por IP/Usuário.

### 🛡️ API8:2023 - Security Misconfiguration & Logging Leakage
- **Proteção**: Redação automática de PII (CPF, Tokens, Senhas) nos logs do Serilog via `PiiRedactionEnricher`.

