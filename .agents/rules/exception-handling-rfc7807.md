# Rule: Exception Handling & RFC 7807 ProblemDetails — FinanceHub

> **Prioridade**: OBRIGATÓRIA (Sempre Enforçada)  
> **Escopo**: Todos os microsserviços do FinanceHub (`src/Services/*`).

---

## 🎯 1. Princípio do Tratamento de Exceções

No **FinanceHub**, o tratamento de erros segue três diretrizes fundamentais integradas:
1. **Hierarquia de Exceções de Domínio**: Erros de negócio lançam exceções fortemente tipadas derivadas de `DomainException`, contendo `ErrorCode` e `StatusCode` HTTP sugerido.
2. **Criação Orientada a TDD ([`tdd-workflow.md`](file:///mnt/c/Code/FinanceHub/.agents/rules/tdd-workflow.md))**: Toda exceção de domínio é criada e validada **no passo 🔴 RED do TDD**, garantindo que seus testes unitários asserção exatamente a mensagem (default ou parametrizada) e o tipo da exceção.
3. **Tratamento Global com RFC 7807**: NENHUM endpoint Minimal API ou Handler deve usar blocos `try/catch` para gerar respostas HTTP de erro. O middleware nativo `IExceptionHandler` do .NET 10 intercepta a exceção e devolve uma resposta estruturada **RFC 7807 (`ProblemDetails`)**.


---

## 🏛️ 2. Hierarquia de Exceções de Domínio

Toda exceção de negócio deve herdar de `DomainException`:

```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected DomainException(string message, string errorCode, int statusCode = 400) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
```

### 2.1 Regra Estrita: Uma Classe Dedicada por Mensagem de Erro (Proibido Strings Brutas)

- **PROIBIDO**: Passar strings brutas de erro em exceções genéricas (ex: `throw new Exception("...")` ou `throw new DomainException("...")`).
- **OBRIGATÓRIO**: Cada validação ou mensagem de erro distinta **DEVE ser uma classe própria derivada de `DomainException`**, encapsulando sua mensagem default, `ErrorCode` e `StatusCode`.

```csharp
// Exceção pronta para AccessToken nulo ou vazio
public class NullOrEmptyAccessTokenDomainException : DomainException
{
    public NullOrEmptyAccessTokenDomainException() 
        : base("AccessToken não pode ser vazio para autorização.", "NULL_OR_EMPTY_ACCESS_TOKEN", statusCode: 400) { }
}

// Exceção pronta para RefreshToken nulo ou vazio
public class NullOrEmptyRefreshTokenDomainException : DomainException
{
    public NullOrEmptyRefreshTokenDomainException() 
        : base("RefreshToken não pode ser vazio para autorização.", "NULL_OR_EMPTY_REFRESH_TOKEN", statusCode: 400) { }
}

// Exceção pronta para ExternalConsentId nulo ou vazio
public class NullOrEmptyExternalConsentIdDomainException : DomainException
{
    public NullOrEmptyExternalConsentIdDomainException() 
        : base("ExternalConsentId não pode ser nulo ou vazio.", "NULL_OR_EMPTY_EXTERNAL_CONSENT_ID", statusCode: 400) { }
}

// Exceção pronta para UserId nulo ou vazio
public class NullOrEmptyUserIdDomainException : DomainException
{
    public NullOrEmptyUserIdDomainException(string? userId = null) 
        : base(
            string.IsNullOrWhiteSpace(userId) 
                ? "UserId não pode ser nulo ou vazio." 
                : $"UserId '{userId}' não é válido.", 
            "INVALID_USER_ID", 
            statusCode: 400) { }
}

// Exceção pronta para InstitutionId nulo ou vazio
public class NullOrEmptyInstitutionIdDomainException : DomainException
{
    public NullOrEmptyInstitutionIdDomainException(string? institutionId = null) 
        : base(
            string.IsNullOrWhiteSpace(institutionId) 
                ? "InstitutionId não pode ser nulo ou vazio." 
                : $"Instituição bancária '{institutionId}' não é suportada.", 
            "INVALID_INSTITUTION_ID", 
            statusCode: 400) { }
}

// Exceção parametrizável para estado inválido do consentimento
public class ConsentInvalidStateException : DomainException
{
    public ConsentInvalidStateException(string currentStatus, string targetAction) 
        : base($"Consentimento no estado '{currentStatus}' não pode executar a ação '{targetAction}'.", "CONSENT_INVALID_STATE", statusCode: 409) { }
}
```


---


## 🛠️ 3. Middleware Global `IExceptionHandler` (.NET 10)

Cada microsserviço deve registrar o `GlobalExceptionHandler` no `Program.cs`:

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

app.UseExceptionHandler();
```

### Formato da Resposta JSON RFC 7807:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Erro de Negócio",
  "status": 409,
  "detail": "Consentimento no estado Revoked não pode ser alterado para Authorized.",
  "instance": "/api/v1/consents/123/authorize",
  "errorCode": "CONSENT_INVALID_STATE",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

---

## 📋 4. Exceções no Planejamento e Especificação

Toda especificação em `.agents/specs/` **deve definir a tabela de mapeamento de exceções e status HTTP da funcionalidade**:

| Exceção | Condição de Disparo | Status HTTP | ErrorCode |
|---------|─────────────────────|-------------|-----------|
| `ConsentDomainException` | Invariante de negócio violada | 400 | `INVALID_CONSENT_DATA` |
| `ConsentNotFoundException` | Consentimento não localizado | 404 | `CONSENT_NOT_FOUND` |
| `ConsentInvalidStateException` | Transição de estado proibida | 409 | `CONSENT_INVALID_STATE` |
