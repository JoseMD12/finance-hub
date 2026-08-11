# Rule: DDD Aggregate Root & Rich Domain Model — FinanceHub

> **Prioridade**: OBRIGATÓRIA (Sempre Enforçada)  
> **Escopo**: Todas as camadas de Domínio (`*.Domain`) de todos os microsserviços.

---

## 🏛️ 1. Princípio do Domínio Rico (Rich Domain Model)

Proibido a criação de **Modelos Anêmicos** (classes com propriedades `get; set;` públicas sem comportamentos ou métodos de negócio).

### Regras Mandatórias:
1. **Encapsulamento Estrito**:
   - Todas as propriedades das entidades e Value Objects **devem ter `private set` ou `init`**.
   - Construtores de entidades são privados ou protegidos (`protected`), utilizando **Factories estáticas de negócio** (ex: `BankConsent.Create(...)`) para instanciação válida.
2. **Métodos de Negócio Expressivos**:
   - Toda alteração de estado deve ser realizada exclusivamente por métodos que representem ações de domínio da linguagem ubíqua (ex: `Authorize()`, `RotateTokens()`, `Revoke()`).
   - Proibido métodos genéricos de alteração como `SetStatus()` ou `UpdateData()`.
3. **Validação de Invariantes In-Entity**:
   - Todas as regras e validações de integridade do modelo de negócio (ex: expiração de token, valores nulos, estados inválidos) **devem residir dentro da própria Entidade ou Value Object**, disparando `DomainException` em descumprimentos.

---

## 🛡️ 2. Padrão Aggregate Root (Raiz do Agregado)

O acesso e a modificação do estado de qualquer elemento do domínio ocorrem **exclusivamente através da Raiz do Agregado (Aggregate Root)**.

### Regras Mandatórias:
1. **Ponto Único de Entrada**:
   - Repositórios persistem e recuperam **apenas Raízes do Agregado** (ex: `IBankConsentRepository` apenas lida com a raiz `BankConsent`). Entidades internas ou Value Objects (ex: `ConsentToken`) **nunca possuem repositórios próprios**.
2. **Gerenciamento de Entidades e Value Objects Internos**:
   - Entidades e Value Objects internos são imutáveis externamente e manipulados estritamente por métodos da Raiz do Agregado.
   - Exemplo: para atualizar tokens, o chamador invoca `consentAggregate.RotateTokens(...)` na raiz, e a raiz gerencia a substituição imutável do Value Object `ConsentToken`.

---

## 📢 3. Eventos de Domínio (Domain Events)

1. A Raiz do Agregado acumula eventos de domínio internos em uma lista privada `_domainEvents`.
2. Métodos de negócio registram eventos (ex: `AddDomainEvent(new ConsentAuthorizedDomainEvent(Id, ...))`).
3. Repositórios ou Handlers despaçam os eventos de domínio após a transação persistir no banco.

---

## ❌ Exemplo Incorreto (Modelo Anêmico - PROIBIDO)

```csharp
// ❌ PROIBIDO: Classe anêmica sem comportamentos e com setters públicos
public class BankConsent
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

## ✅ Exemplo Correto (Domínio Rico & Aggregate Root - OBRIGATÓRIO)

```csharp
// ✅ OBRIGATÓRIO: Raiz do Agregado rica e totalmente encapsulada
public class BankConsent : AggregateRoot
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string InstitutionId { get; private set; }
    public ConsentToken Token { get; private set; } // Value Object interno
    public ConsentStatus Status { get; private set; }

    private BankConsent() { } // Para EF Core

    public static BankConsent Request(string userId, string institutionId, string consentId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ConsentDomainException("UserId não pode ser nulo ou vazio.");

        return new BankConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InstitutionId = institutionId,
            Token = ConsentToken.CreatePending(consentId),
            Status = ConsentStatus.Pending
        };
    }

    public void Authorize(string accessToken, string refreshToken, int expiresInSeconds)
    {
        if (Status != ConsentStatus.Pending)
            throw new ConsentDomainException($"Consentimento no estado {Status} não pode ser autorizador.");

        Token = ConsentToken.CreateAuthorized(accessToken, refreshToken, expiresInSeconds);
        Status = ConsentStatus.Authorized;

        AddDomainEvent(new ConsentAuthorizedDomainEvent(Id, UserId, InstitutionId));
    }
}
```
