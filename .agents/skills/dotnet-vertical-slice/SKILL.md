---
name: dotnet-vertical-slice
description: Scaffolding guide for building vertical slice features in FinanceHub (.NET 10), including Use Cases, Minimal API endpoints, Command/Query handlers, FluentValidation, and financial reconciliation endpoints.
---

# .NET 10 Vertical Slice Architecture Guide

This guide provides explicit patterns for scaffolding new features using **Vertical Slice Architecture** in **FinanceHub** (.NET 10 / C# 13). Features are organized around domain business capabilities within their respective microservices rather than technical layers.

---

## 1. Directory Structure Conventions

Each feature (Use Case) lives in its own self-contained directory under the target microservice project, e.g., `src/Services/FinanceHub.TransactionAggregator/Features/<DomainGroup>/<UseCaseName>/`.

```text
src/Services/FinanceHub.TransactionAggregator/Features/Transactions/
└── ReconcileTransactions/
    ├── ReconcileTransactionsEndpoint.cs   // Minimal API Route Mapping
    ├── ReconcileTransactionsCommand.cs    // Request & Response Records (CQRS)
    ├── ReconcileTransactionsHandler.cs    // Business Logic / Mediator Handler
    ├── ReconcileTransactionsValidator.cs  // FluentValidation Rules
    └── ReconcileTransactionsModels.cs     // Response DTOs & Enums
```

---

## 2. Step-by-Step Feature Scaffolding Workflow

### Step 1: Define Command/Query Request & Response
Use immutable `record` types with explicit property types (e.g. `decimal` for monetary values, `DateTimeOffset` for timestamps).

```csharp
namespace FinanceHub.TransactionAggregator.Features.Transactions.ReconcileTransactions;

public record ReconcileTransactionsCommand(
    Guid AccountId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    IReadOnlyCollection<ExternalTransactionItem> ExternalTransactions
) : IRequest<Result<ReconcileTransactionsResult>>;

public record ExternalTransactionItem(
    string ExternalId,
    decimal Amount,
    string Currency,
    DateTimeOffset TransactionDate,
    string Description,
    string? FitId
);

public record ReconcileTransactionsResult(
    Guid BatchId,
    int MatchedCount,
    int UnmatchedInternalCount,
    int UnmatchedExternalCount,
    decimal TotalMatchedAmount,
    IReadOnlyCollection<ReconciliationDiscrepancy> Discrepancies
);

public record ReconciliationDiscrepancy(
    string Type, // e.g. "AmountMismatch", "UnmatchedExternal", "UnmatchedInternal"
    string Description,
    decimal DiscrepancyAmount,
    Guid? InternalTransactionId,
    string? ExternalTransactionId
);
```

### Step 2: Implement Request Validator (FluentValidation)
Add strict validation rules to protect domain invariants prior to command execution:

```csharp
namespace FinanceHub.TransactionAggregator.Features.Transactions.ReconcileTransactions;

public sealed class ReconcileTransactionsValidator : AbstractValidator<ReconcileTransactionsCommand>
{
    public ReconcileTransactionsValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId is required.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("StartDate must be before or equal to EndDate.");

        RuleFor(x => x.ExternalTransactions)
            .NotNull();

        RuleForEach(x => x.ExternalTransactions).ChildRules(item =>
        {
            item.RuleFor(t => t.ExternalId).NotEmpty();
            item.RuleFor(t => t.Amount).NotEqual(0).WithMessage("Transaction amount cannot be zero.");
            item.RuleFor(t => t.Currency).Length(3).WithMessage("Currency must be a valid 3-letter ISO code.");
        });
    }
}
```

### Step 3: Implement Business Logic Handler
Inject domain repositories, microservice DbContext (`TransactionAggregatorDbContext`), or services. Enforce explicit transactional boundaries for financial integrity.

```csharp
namespace FinanceHub.TransactionAggregator.Features.Transactions.ReconcileTransactions;

public sealed class ReconcileTransactionsHandler 
    : IRequestHandler<ReconcileTransactionsCommand, Result<ReconcileTransactionsResult>>
{
    private readonly TransactionAggregatorDbContext _dbContext;
    private readonly ILogger<ReconcileTransactionsHandler> _logger;

    public ReconcileTransactionsHandler(
        TransactionAggregatorDbContext dbContext,
        ILogger<ReconcileTransactionsHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ReconcileTransactionsResult>> Handle(
        ReconcileTransactionsCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reconciliation for AccountId: {AccountId}", request.AccountId);

        // Fetch internal ledger transactions for date range
        var internalTransactions = await _dbContext.Transactions
            .Where(t => t.AccountId == request.AccountId 
                     && t.TransactionDate >= request.StartDate 
                     && t.TransactionDate <= request.EndDate)
            .ToListAsync(cancellationToken);

        var discrepancies = new List<ReconciliationDiscrepancy>();
        int matchedCount = 0;
        decimal matchedTotal = 0m;

        // Reconciliation matching logic
        var unmatchedInternal = internalTransactions.ToDictionary(t => t.Id);
        
        foreach (var ext in request.ExternalTransactions)
        {
            // Match rule: Exact amount + date within +/- 2 days + matching FitID or description key
            var match = internalTransactions.FirstOrDefault(i => 
                unmatchedInternal.ContainsKey(i.Id) &&
                i.Amount == ext.Amount &&
                Math.Abs((i.TransactionDate - ext.TransactionDate).TotalHours) <= 48);

            if (match != null)
            {
                matchedCount++;
                matchedTotal += match.Amount;
                unmatchedInternal.Remove(match.Id);
            }
            else
            {
                discrepancies.Add(new ReconciliationDiscrepancy(
                    Type: "UnmatchedExternal",
                    Description: $"External transaction {ext.ExternalId} has no matching ledger entry.",
                    DiscrepancyAmount: ext.Amount,
                    InternalTransactionId: null,
                    ExternalTransactionId: ext.ExternalId
                ));
            }
        }

        foreach (var remaining in unmatchedInternal.Values)
        {
            discrepancies.Add(new ReconciliationDiscrepancy(
                Type: "UnmatchedInternal",
                Description: $"Ledger transaction {remaining.Id} was not found in bank feed.",
                DiscrepancyAmount: remaining.Amount,
                InternalTransactionId: remaining.Id,
                ExternalTransactionId: null
            ));
        }

        var result = new ReconcileTransactionsResult(
            BatchId: Guid.NewGuid(),
            MatchedCount: matchedCount,
            UnmatchedInternalCount: unmatchedInternal.Count,
            UnmatchedExternalCount: discrepancies.Count(d => d.Type == "UnmatchedExternal"),
            TotalMatchedAmount: matchedTotal,
            Discrepancies: discrepancies
        );

        return Result.Success(result);
    }
}
```

### Step 4: Map Minimal API Endpoint (.NET 10)
Map endpoints explicitly in `Endpoint.cs` using .NET 10 Minimal API style with OpenAPI metadata and typed results:

```csharp
namespace FinanceHub.TransactionAggregator.Features.Transactions.ReconcileTransactions;

public static class ReconcileTransactionsEndpoint
{
    public static void MapReconcileTransactionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/accounts/{accountId:guid}/reconcile", async (
            Guid accountId,
            ReconcileTransactionsCommand command,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (accountId != command.AccountId)
            {
                return Results.BadRequest("Route accountId does not match request body.");
            }

            var result = await mediator.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.Problem(detail: result.Error.Message, statusCode: StatusCodes.Status400BadRequest);
        })
        .WithName("ReconcileTransactions")
        .WithTags("Transactions", "Reconciliation")
        .Produces<ReconcileTransactionsResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}
```

---

## 3. Financial Reconciliation Specific Guidelines

Reconciliation endpoints must implement auditability and deterministic state matching:
1. **Idempotency**: Requests must support an `Idempotency-Key` HTTP header to prevent duplicate reconciliation batch creation.
2. **Audit Logging**: Store batch outcomes in microservice-scoped `ReconciliationBatches` and `ReconciliationItems` tables.
3. **Tolerance Config**: Allow configurable matching tolerance thresholds (e.g. date skew tolerance, FX conversion spread).

---

## 4. Verification Checklist

1. **Endpoint Auto-Discovery**: Register endpoint in `Program.cs` / `MapEndpoints()`.
2. **Validator Unit Tests**: Add test cases covering invalid currency codes, empty transaction arrays, and mismatched dates.
3. **Integration Test**: Verify endpoint behavior using `WebApplicationFactory<Program>` in `tests/FinanceHub.TransactionAggregator.Tests/`.
4. **Execution**: Run `dotnet build` and `dotnet test`.

