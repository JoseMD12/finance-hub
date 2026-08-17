using Bogus;
using FinanceHub.Shared.Messaging.Events;

namespace FinanceHub.UnitTests.Factories;

/// <summary>
/// Factory centralizada para geração de dados de teste realistas para TransactionIngested.
/// </summary>
public static class FakeTransactionIngested
{
    private static readonly Faker _faker = new("pt_BR");

    public static TransactionIngested Build(string source = "Itau", string userId = "user-123") => new(
        IngestionId: Guid.NewGuid(),
        UserId: userId,
        Source: source,
        AccountId: _faker.Random.AlphaNumeric(8),
        BankTransactionId: _faker.Random.AlphaNumeric(16),
        Amount: _faker.Finance.Amount(1, 5000),
        TransactionDate: _faker.Date.Recent(30),
        Description: _faker.Commerce.ProductName(),
        Currency: "BRL",
        RawPayloadJson: "{}",
        OccurredAtUtc: DateTime.UtcNow
    );

    public static IEnumerable<TransactionIngested> BuildMany(int count = 5, string source = "Itau", string userId = "user-123") =>
        Enumerable.Range(0, count).Select(_ => Build(source, userId));
}
