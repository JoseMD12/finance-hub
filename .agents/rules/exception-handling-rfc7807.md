# Rule: Exception Handling & RFC 7807 ProblemDetails — FinanceHub

> **Prioridade**: OBRIGATÓRIA (Sempre Enforçada)  
> **Escopo**: Todos os microsserviços do FinanceHub (`src/Services/*`).

---

## 🎯 1. Princípio do Tratamento de Exceções

No **FinanceHub**, o tratamento de erros segue duas diretrizes fundamentais:
1. **Hierarquia de Exceções de Domínio**: Erros de negócio lançam exceções fortemente tipadas derivadas de `DomainException`, contendo `ErrorCode` e `StatusCode` HTTP sugerido.
2. **Tratamento Global com RFC 7807**: NENHUM endpoint Minimal API ou Handler deve usar blocos `try/catch` para gerar respostas HTTP de erro. O middleware nativo `IExceptionHandler` do .NET 10 intercepta a exceção e devolve uma resposta estruturada **RFC 7807 (`ProblemDetails`)**.

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

### Regras de Herança:
- `ConsentNotFoundException`: herda com `statusCode: 404`, `errorCode: "CONSENT_NOT_FOUND"`.
- `ConsentInvalidStateException`: herda com `statusCode: 409`, `errorCode: "CONSENT_INVALID_STATE"`.
- `UnauthorizedBankException`: herda com `statusCode: 401`, `errorCode: "UNAUTHORIZED_BANK_ACCESS"`.

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
