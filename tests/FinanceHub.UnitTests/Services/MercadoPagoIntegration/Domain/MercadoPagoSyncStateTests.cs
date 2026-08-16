using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Domain;

public class MercadoPagoSyncStateTests
{
    private readonly FakeTimeProvider _timeProvider;

    public MercadoPagoSyncStateTests()
    {
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var initialCursor = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-30);

        // Act
        var state = MercadoPagoSyncState.Create("user-01", "collector-01", initialCursor, _timeProvider);

        // Assert
        state.Id.Should().NotBeEmpty();
        state.UserId.Should().Be("user-01");
        state.AccountId.Should().Be("collector-01");
        state.LastSyncCursorUtc.Should().Be(initialCursor);
        state.Status.Should().Be(SyncExecutionStatus.Idle);
        state.IngestedCount.Should().Be(0);
        state.CreatedAtUtc.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
    }

    [Theory]
    [InlineData("", "acc-1")]
    [InlineData("   ", "acc-1")]
    [InlineData("user-1", "")]
    [InlineData("user-1", "   ")]
    public void Create_WithEmptyUserIdOrAccountId_ShouldThrowDomainException(string userId, string accountId)
    {
        // Arrange
        var initialCursor = _timeProvider.GetUtcNow().UtcDateTime;

        // Act
        var act = () => MercadoPagoSyncState.Create(userId, accountId, initialCursor, _timeProvider);

        // Assert
        act.Should().Throw<NullOrEmptyMercadoPagoCredentialsDomainException>();
    }

    [Fact]
    public void StartSync_ShouldTransitionToInProgress()
    {
        // Arrange
        var state = MercadoPagoSyncState.Create("user-01", "collector-01", DateTime.UtcNow.AddDays(-1), _timeProvider);
        _timeProvider.Advance(TimeSpan.FromMinutes(10));

        // Act
        state.StartSync(_timeProvider);

        // Assert
        state.Status.Should().Be(SyncExecutionStatus.InProgress);
        state.LastExecutionUtc.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void CompleteSync_ShouldUpdateCursorAndStatus()
    {
        // Arrange
        var initialCursor = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10);
        var state = MercadoPagoSyncState.Create("user-01", "collector-01", initialCursor, _timeProvider);
        var newCursor = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-1);

        // Act
        state.CompleteSync(newCursor, 15, _timeProvider);

        // Assert
        state.Status.Should().Be(SyncExecutionStatus.Completed);
        state.LastSyncCursorUtc.Should().Be(newCursor);
        state.IngestedCount.Should().Be(15);
    }

    [Fact]
    public void FailSync_ShouldSetStatusToFailedAndRecordErrorMessage()
    {
        // Arrange
        var state = MercadoPagoSyncState.Create("user-01", "collector-01", DateTime.UtcNow, _timeProvider);

        // Act
        state.FailSync("Network error 502", _timeProvider);

        // Assert
        state.Status.Should().Be(SyncExecutionStatus.Failed);
        state.LastErrorMessage.Should().Be("Network error 502");
    }
}
