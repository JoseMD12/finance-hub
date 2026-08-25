using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Services;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Unit.PluggyIntegration;

public class SyncJobStoreTests
{
    private readonly ISyncJobStore _store = new InMemorySyncJobStore();

    [Fact]
    public void CreateJob_ShouldRegisterJobWithProcessingStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        const string userId = "user-123";

        // Act
        var job = _store.CreateJob(jobId, userId);

        // Assert
        job.Should().NotBeNull();
        job.JobId.Should().Be(jobId);
        job.Status.Should().Be("Processing");
        job.Message.Should().Contain("segundo plano");
        job.StartedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        job.CompletedAtUtc.Should().BeNull();
        job.Result.Should().BeNull();
    }

    [Fact]
    public void GetJob_WhenJobDoesNotExist_ShouldReturnNull()
    {
        // Act
        var job = _store.GetJob(Guid.NewGuid());

        // Assert
        job.Should().BeNull();
    }

    [Fact]
    public void SetCompleted_ShouldUpdateJobStatusAndResult()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _store.CreateJob(jobId, "user-123");

        var summary = new SyncPluggySummaryDto(
            TotalItemsSynced: 3,
            TotalAccountsSynced: 6,
            TotalCheckingTransactionsIngested: 100,
            TotalCardTransactionsIngested: 50,
            SyncedAtUtc: DateTime.UtcNow
        );

        // Act
        var updated = _store.SetCompleted(jobId, summary);
        var retrieved = _store.GetJob(jobId);

        // Assert
        updated.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
        retrieved.CompletedAtUtc.Should().NotBeNull();
        retrieved.Result.Should().BeEquivalentTo(summary);
    }

    [Fact]
    public void SetFailed_ShouldUpdateJobStatusAndErrorMessage()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _store.CreateJob(jobId, "user-123");
        const string errorMessage = "Pluggy API 500 Internal Error";

        // Act
        var updated = _store.SetFailed(jobId, errorMessage);
        var retrieved = _store.GetJob(jobId);

        // Assert
        updated.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Failed");
        retrieved.ErrorMessage.Should().Be(errorMessage);
        retrieved.CompletedAtUtc.Should().NotBeNull();
    }
}
