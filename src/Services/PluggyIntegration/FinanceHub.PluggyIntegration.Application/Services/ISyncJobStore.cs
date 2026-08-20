using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Services;

public interface ISyncJobStore
{
    SyncJobStatusDto CreateJob(Guid jobId, string userId);
    SyncJobStatusDto? GetJob(Guid jobId);
    bool SetCompleted(Guid jobId, SyncPluggySummaryDto result);
    bool SetFailed(Guid jobId, string errorMessage);
}
