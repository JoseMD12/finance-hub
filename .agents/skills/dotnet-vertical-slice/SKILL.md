---
name: dotnet-vertical-slice
description: Scaffolding guide for building clean CQRS vertical use-case slices in FinanceHub (.NET 10), covering Application Commands/Queries, Handler Interfaces, Rich Domain Models, and Minimal API endpoints.
---

# .NET 10 Clean Architecture & CQRS Use-Case Guide — FinanceHub

This guide provides explicit patterns for scaffolding new features (Use Cases) in **FinanceHub** (.NET 10 / C# 13). Features follow Clean Architecture and DDD principles per microservice, structured around CQRS Commands/Queries, Handler interfaces (DIP), Rich Domain Models, and Minimal API endpoints.

---

## 1. Directory Structure Conventions

In FinanceHub microservices, code is organized by Clean Architecture layers with clear vertical Use-Case slicing inside Application and API projects:

```text
src/Services/FinanceHub.<ServiceName>/
├── FinanceHub.<ServiceName>.Domain/
│   ├── Entities/                 // Aggregate Roots & Rich Domain Entities
│   ├── ValueObjects/              // Immutable Value Objects (Records)
│   ├── Exceptions/                // Strongly-typed Domain Exceptions
│   └── Events/                    // Domain Events
├── FinanceHub.<ServiceName>.Application/
│   ├── Commands/                  // Command DTOs & Handlers
│   │   └── IngestTransaction/
│   │       ├── IngestTransactionCommand.cs
│   │       ├── IngestTransactionCommandHandler.cs
│   │       └── IIngestTransactionCommandHandler.cs
│   ├── Queries/                   // Query DTOs & Handlers
│   │   └── GetTransactions/
│   │       ├── GetTransactionsQuery.cs
│   │       ├── GetTransactionsQueryHandler.cs
│   │       └── IGetTransactionsQueryHandler.cs
│   ├── Interfaces/                // Repository & Infrastructure Interfaces
│   └── DTOs/                      // Output/Read DTOs
├── FinanceHub.<ServiceName>.Infrastructure/
│   ├── Persistence/               // DbContext, EF Core Configurations & Repositories
│   └── DependencyInjection.cs
└── FinanceHub.<ServiceName>.Api/
    ├── Endpoints/                 // Minimal API Endpoint Mappings
    ├── Middleware/                // GlobalExceptionHandler (RFC 7807)
    ├── Program.cs                 // Top-Level Statements + namespaced Program
    └── DependencyInjection.cs
```

---

## 2. Architectural Guidelines & Hard Rules

1. **Rich Domain Model (No Anemic Entities)**:
   - Invariants and validations MUST be encapsulated 100% inside Domain Aggregate Roots and Value Objects.
   - Domain errors throw strongly-typed exceptions derived from `DomainException` (e.g. `InvalidCurrencyDomainException`, `TransactionNotFoundDomainException`).
   - Zero FluentValidation classes — validation logic resides exclusively in the Domain layer.

2. **Strict Dependency Inversion Principle (DIP)**:
   - Every Command Handler and Query Handler MUST define and implement a dedicated interface (e.g. `IIngestTransactionCommandHandler`, `IGetTransactionsQueryHandler`).
   - Endpoints MUST inject Handler interfaces (`IIngestTransactionCommandHandler`), never concrete implementation classes.

3. **Global Exception Handling (RFC 7807 ProblemDetails)**:
   - APIs handle domain and system exceptions globally via `IExceptionHandler` (`GlobalExceptionHandler`).
   - Responses return RFC 7807 `ProblemDetails` with `traceId` and `errorCode`. Endpoints perform ZERO manual try/catch blocks.

4. **Environment Configuration Loading**:
   - Environment variables must be loaded from `.env` via `DotNetEnv.Env.TraversePath().Load()` on `Program.cs` startup.
   - Zero inline hardcoded fallback defaults in code. Fail-fast if required environment variables are missing.

---

## 3. Step-by-Step Feature Scaffolding Workflow

### Step 1: Define Domain Logic & Exception (Domain Layer)
Encapsulate invariants directly inside Aggregate Root or Value Objects:

```csharp
namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class CanonicalTransaction
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public Money Amount { get; private set; }
    public Guid CategoryId { get; private set; }

    public void CategorizeManually(Guid newCategoryId)
    {
        if (newCategoryId == Guid.Empty)
        {
            throw new InvalidCategoryIdDomainException();
        }

        CategoryId = newCategoryId;
    }
}
```

### Step 2: Define Command/Query & Handler Interface (Application Layer)

```csharp
namespace FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;

public record CategorizeTransactionCommand(
    Guid TransactionId,
    string UserId,
    Guid NewCategoryId,
    bool CreateCustomRule);

public interface ICategorizeTransactionCommandHandler
{
    Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken);
}
```

### Step 3: Implement Handler Class (Application Layer)

```csharp
namespace FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;

public class CategorizeTransactionCommandHandler : ICategorizeTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public CategorizeTransactionCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task Handle(CategorizeTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null)
        {
            throw new TransactionNotFoundDomainException(command.TransactionId);
        }

        transaction.CategorizeManually(command.NewCategoryId);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
```

### Step 4: Map Minimal API Endpoint (API Layer)

```csharp
namespace FinanceHub.TransactionAggregator.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/transactions")
            .WithTags("Transactions");

        group.MapPatch("/{id:guid}/categorize", async (
            Guid id,
            CategorizeTransactionRequest request,
            ICategorizeTransactionCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CategorizeTransactionCommand(
                id,
                request.UserId,
                request.NewCategoryId,
                request.CreateCustomRule);

            await handler.Handle(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("CategorizeTransaction")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
```

---

## 4. Verification Checklist

1. **Interface Binding**: Register interface + implementation in `DependencyInjection.cs` (`services.AddScoped<ICategorizeTransactionCommandHandler, CategorizeTransactionCommandHandler>();`).
2. **Unit Tests**: Add unit tests for Domain entity methods, Application Handlers (with `NSubstitute` mocks), and API endpoints (`WebApplicationFactory`).
3. **Execution**: Run `dotnet test` to confirm 100% GREEN build and test execution.
