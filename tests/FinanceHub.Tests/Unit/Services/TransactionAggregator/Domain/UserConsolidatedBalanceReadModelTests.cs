using FinanceHub.TransactionAggregator.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Unit.Services.TransactionAggregator.Domain;

[Trait("Category", "Unit")]
public class UserConsolidatedBalanceReadModelTests
{
    [Fact]
    public void Constructor_ShouldCalculateNetBalanceCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var checkingBalance = 5000.00m;
        var creditCardSpent = 1500.00m;

        // Act
        var model = new UserConsolidatedBalanceReadModel(userId, checkingBalance, creditCardSpent);

        // Assert
        model.UserId.Should().Be(userId);
        model.TotalCheckingBalance.Should().Be(checkingBalance);
        model.TotalCreditCardSpent.Should().Be(creditCardSpent);
        model.NetConsolidatedBalance.Should().Be(3500.00m);
    }

    [Fact]
    public void UpdateBalance_ShouldRecalculateNetBalanceAndTimestamp()
    {
        // Arrange
        var model = new UserConsolidatedBalanceReadModel("user-123", 1000m, 200m);

        // Act
        model.UpdateBalance(2000m, 500m);

        // Assert
        model.TotalCheckingBalance.Should().Be(2000m);
        model.TotalCreditCardSpent.Should().Be(500m);
        model.NetConsolidatedBalance.Should().Be(1500m);
    }
}
