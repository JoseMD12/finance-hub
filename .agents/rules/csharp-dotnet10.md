# Modern C# 13 and .NET 10 Guidelines

This document defines mandatory guidelines and code formatting standards for modern C# 13 and .NET 10 development within **FinanceHub**.

---

## 1. Primary Constructors
Use primary constructors for classes, structs, and records to eliminate boilerplate dependency injection and field assignments.

### Guidelines
* Standardize class dependency injection using primary constructors.
* Mark injected service dependencies as `private readonly` explicitly when captured by internal members if state encapsulation is required, or rely on primary constructor parameter scope.
* For records, use positional primary constructors for concise immutable data representation.

```csharp
// Recommended: Primary Constructor for Services
public sealed class TransactionService(
    ITransactionRepository repository,
    ILogger<TransactionService> logger,
    IDateTimeProvider dateTimeProvider) : ITransactionService
{
    public async Task<TransactionResult> ProcessAsync(CreateTransactionCommand command, CancellationToken ct)
    {
        logger.LogInformation("Processing transaction {TransactionId}", command.Id);
        // ... implementation
    }
}
```

---

## 2. Collection Expressions and Spread Operator
Always prefer C# 12/13 collection expressions `[...]` and spread syntax `..` over array instantiation (`new T[]`), `List<T>` constructors, or `Array.Empty<T>()`.

### Guidelines
* Use `[]` for empty collection initialization.
* Use `[item1, item2]` for inline collection construction.
* Use spread operator `[.. collection1, .. collection2]` for concatenating or passing sequences.
* Utilize `params` collections in C# 13 for ref-safe or allocation-friendly varargs APIs (e.g., `params ReadOnlySpan<T>`).

```csharp
// Empty collections
IEnumerable<Transaction> emptyTransactions = [];
List<string> emptyTags = [];

// Collection concatenation
ReadOnlySpan<string> coreAccountTypes = ["Checking", "Savings"];
ReadOnlySpan<string> investmentAccountTypes = ["Brokerage", "Crypto"];
string[] allAccountTypes = [.. coreAccountTypes, .. investmentAccountTypes, "Escrow"];

// C# 13 params collections
public void ProcessBatch(params ReadOnlySpan<Guid> transactionIds)
{
    foreach (ref readonly var id in transactionIds)
    {
        // Allocation-free iteration
    }
}
```

---

## 3. Pattern Matching
Leverage pattern matching to replace complex `if-else` cascades, type checks, and null assertions.

### Guidelines
* Use **Property Patterns** for state checks.
* Use **Positional Patterns** with tuple deconstruction.
* Use **List Patterns** for sequence structure matching.
* Use relational patterns for value ranges (e.g., risk scoring, transaction limits).

```csharp
// Property and Relational Patterns
public static RiskLevel EvaluateTransactionRisk(Transaction tx) => tx switch
{
    { Amount: > 50_000, Account.IsVerified: false } => RiskLevel.Critical,
    { Amount: > 10_000 } or { Account.CountryCode: not "BRA" } => RiskLevel.High,
    { Status: TransactionStatus.Pending, LedgerEntries.Count: 0 } => RiskLevel.Invalid,
    _ => RiskLevel.Low
};

// List Patterns
public static decimal CalculateTieredFee(decimal[] dailyHistory) => dailyHistory switch
{
    [] => 0.00m,
    [var single] => single * 0.01m,
    [.., var secondLast, var last] when last > secondLast * 2 => 15.00m,
    [var first, ..] when first > 100_000m => 5.00m,
    _ => 2.50m
};
```

---

## 4. Record Types & Immutability
Represent domain events, DTOs, messages, and value objects using C# `record` types to ensure value equality and thread-safe immutability.

### Guidelines
* Use `record class` (or simply `record`) for reference-based immutable objects (DTOs, Domain Events, CQRS Commands/Queries).
* Use `readonly record struct` for ultra-lightweight value types created frequently (e.g., Money value object, Currency pair, GeoCoordinate) to avoid heap allocations.
* Leverage `with` expressions for non-destructive mutation.

```csharp
// Lightweight Value Object allocation-free on heap
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Reais(decimal amount) => new(amount, "BRL");
}

// Immutable DTO / Command
public record CreatePaymentCommand(
    Guid IdempotencyKey,
    Money Amount,
    string SourceAccountId,
    string DestinationAccountId) : IRequest<Result<PaymentResponse>>;
```

---

## 5. Minimal APIs & FastEndpoints
Prefer **FastEndpoints** or standard .NET 10 Minimal APIs over heavy MVC Controllers for clear, high-performance vertical slices.

### Guidelines
* Keep endpoints focused: 1 file per endpoint class.
* Use explicit type annotations and typed results (`TypedResults.Ok()`, `TypedResults.Problem()`).
* Enforce route group constraints and automatic OpenAPI/Swagger metadata generation.

```csharp
public class GetAccountBalanceEndpoint : Endpoint<GetAccountBalanceRequest, AccountBalanceResponse>
{
    public override void Configure()
    {
        Get("/api/v1/accounts/{AccountId}/balance");
        Policies("Bearer", "RequireOpenFinanceScope");
        Description(b => b.Produces<AccountBalanceResponse>(200).ProducesProblem(404));
    }

    public override async Task HandleAsync(GetAccountBalanceRequest req, CancellationToken ct)
    {
        var result = await QueryAsync(new GetBalanceQuery(req.AccountId), ct);
        await SendOkAsync(result, ct);
    }
}
```

---

## 6. Async Performance & Memory Efficiency
FinanceHub demands low-latency, non-blocking high-throughput execution.

### Guidelines
* **`CancellationToken` propagation**: Pass `CancellationToken` through every asynchronous call without exception.
* **`ValueTask` usage**: Use `ValueTask<T>` for hot-path asynchronous methods that frequently complete synchronously (e.g., cache hits, inline validations).
* **Memory & Span**: Use `ReadOnlySpan<char>`, `Span<byte>`, and `ArrayPool<T>` in message parsing, token validation, and string manipulations to prevent GC pressure.
* **Avoid `async void`**: Use `async void` ONLY in event handlers (if any); use `Task` or `ValueTask` everywhere else.
* **Avoid `Task.Result` / `.Wait()`**: Never block on asynchronous calls; sync-over-async causes thread pool starvation.

```csharp
// ValueTask hot-path optimization
public ValueTask<AccountCacheEntry?> GetAccountFromCacheAsync(string accountId, CancellationToken ct = default)
{
    if (_localMemoryCache.TryGetValue(accountId, out AccountCacheEntry? entry))
    {
        return ValueTask.FromResult(entry); // Zero allocation
    }

    return FetchFromDistributedCacheAsync(accountId, ct);
}
```

---

## 7. Arquitetura & Manutenibilidade (.NET 10 Best Practices)

### 7.1 TimeProvider Nativo (.NET 10)
- **Regra**: Nunca chamar `DateTime.UtcNow` diretamente em lógicas de expiração de token ou regras de negócio. Injetar o `TimeProvider` nativo.
- **Motivação**: Permitir testes TDD ultra-rápidos manipulando a passagem do tempo com `FakeTimeProvider` sem delays reais.

### 7.2 Keyed Services Nativos (`AddKeyedScoped`)
- **Regra**: Registrar estratégias de bancos usando Keyed Services do .NET 10 em vez de factories manuais com `switch/case`:
  ```csharp
  builder.Services.AddKeyedScoped<IOOAuthBankClientStrategy, ItauOAuthStrategy>("itau");
  builder.Services.AddKeyedScoped<IOOAuthBankClientStrategy, MercadoPagoOAuthStrategy>("mercadopago");
  ```

### 7.3 Pattern `Result<T>`
- **Regra**: Usar `Result<T>` ou `Result` em Use Cases em vez de exceções para fluxos previsíveis de negócio.

### 7.4 Strongly Typed IDs (`readonly record struct`)
- **Regra**: Usar structs imutáveis para IDs de domínio em vez de `Guid` ou `string` puros (ex: `public readonly record struct ConsentId(Guid Value)`).

### 7.5 Options Validation (`ValidateOnStart`)
- **Regra**: Validar se seções de configuração do `appsettings.json` possuem credenciais antes de subir o serviço usando `ValidateOnStart()`.

### 7.6 Clean Code & Política de Comentários Essenciais
- **Regra de Ouro**: O código limpo deve ser autoexplicativo. Evitar estritamente comentários triviais ou redundantes que apenas parafraseiam a sintaxe do C#.
- **Comentários Proibidos**:
  - `// Arrange`, `// Act`, `// Assert` em arquivos de teste unitário.
  - `// For EF Core`, `// Constructor`, `// Properties`, `// Methods`.
  - Comentários em cima de getters/setters ou exceções óbvias.
- **Comentários Permitidos (Exclusivos)**:
  - Explicações de **motivos não-óbvios** de decisões de arquitetura.
  - Requisitos regulatórios do Banco Central / Open Finance Brasil que exigem lógica específica.

### 7.7 Política de Zero Magic Strings e Zero Magic Numbers
- **Regra de Ouro**: NENHUMA string de prefixo, instituição bancária, ação de token ou número mágico pode ser declarada inline no código (ex: `$"mp-access-{Guid.NewGuid():N}"`).
- **Padrão Obrigatório**:
  - Todas as constantes de identificadores de bancos (ex: `BankIdentifiers.Itau`, `BankIdentifiers.MercadoPago`), prefixos de tokens (ex: `BankPrefixes.MercadoPago`, `BankPrefixes.Itau`) e tipos de ação (ex: `TokenActions.Access`, `TokenActions.Refresh`, `TokenActions.Renewed`) DEVEM ser centralizadas em constantes/estruturas globais no Domínio/Infraestrutura.
- **Motivação**: Evitar duplicação de código, eliminar erros de digitação (typos) e garantir refatorações limpas em toda a solução.

### 7.8 Classes Dedicadas de Injeção de Dependência (`DependencyInjection.cs`)
- **Regra de Ouro**: Cada camada (`Infrastructure`, `Application`, `Api`) DEVE possuir sua própria classe estática de extensão `DependencyInjection.cs` contendo os registros de DI correspondentes.
- **Encapsulamento de Persistência**: O registro do `DbContext` e da string de conexão PostgreSQL DEVE residir 100% na camada de Infraestrutura (`AddInfrastructureServices`). O `Program.cs` deve apenas orquestrar chamando os métodos de extensão.

### 7.9 Gestão Estrita de Variáveis de Ambiente & `.env` (Zero Default Values)
- **Regra de Ouro**: Todas as variáveis de ambiente (strings de conexão, RabbitMQ, portas) DEVEM ser carregadas a partir de um arquivo `.env` ou do ambiente do sistema.
- **PROIBIDO**: Inline default values ou fallbacks hardcoded no C# (ex: `?? "Host=localhost;Database=..."`). Se uma configuração necessária estiver ausente, o serviço DEVE falhar na inicialização (fail-fast) informando a variável faltante.

### 7.10 Arquivos Separados Obrigatórios para Interface e Implementação
- **Regra de Ouro**: Uma interface (`public interface I<Name>`) e sua classe de implementação (`public class <Name>`) NUNCA devem residir no mesmo arquivo `.cs`. Cada uma deve ter seu próprio arquivo dedicado.
- **Estrutura Obrigatória por Use Case**:
  ```
  Commands/<UseCase>/
    ├── <UseCase>Command.cs              ← record Command / Query
    ├── I<UseCase>CommandHandler.cs      ← interface de contrato (arquivo separado)
    └── <UseCase>CommandHandler.cs       ← implementação concreta (arquivo separado)
  ```
- **Referência Canônica**: `FinanceHub.PluggyIntegration.Application` — todos os handlers e interfaces estão em arquivos `.cs` distintos (`ISyncAllPluggyAccountsCommandHandler.cs`, `SyncAllPluggyAccountsCommandHandler.cs`, etc.).
- **Violação**: Co-localizar interface e implementação no mesmo arquivo é uma violação de DIP-001 e falha na auditoria de arquitetura.




