using System.Collections.Concurrent;
using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Services;

public sealed class InMemorySyncJobStore : ISyncJobStore
{
    private readonly ConcurrentDictionary<Guid, SyncJobStatusDto> _jobs = new();

    public SyncJobStatusDto CreateJob(Guid jobId, string userId)
    {
        var job = new SyncJobStatusDto(
            JobId: jobId,
            Status: "Processing",
            Message: "Sincronização em lote iniciada com sucesso em segundo plano.",
            StartedAtUtc: DateTime.UtcNow,
            CompletedAtUtc: null,
            Result: null,
            ErrorMessage: null
        );

        _jobs[jobId] = job;
        return job;
    }

    public SyncJobStatusDto? GetJob(Guid jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    public bool SetCompleted(Guid jobId, SyncPluggySummaryDto result)
    {
        if (!_jobs.TryGetValue(jobId, out var existing))
        {
            return false;
        }

        var completed = existing with
        {
            Status = "Completed",
            Message = "Sincronização concluída com sucesso.",
            CompletedAtUtc = DateTime.UtcNow,
            Result = result,
            ErrorMessage = null
        };

        _jobs[jobId] = completed;
        return true;
    }

    public bool SetFailed(Guid jobId, string errorMessage)
    {
        if (!_jobs.TryGetValue(jobId, out var existing))
        {
            return false;
        }

        var failed = existing with
        {
            Status = "Failed",
            Message = "Falha ao processar sincronização em segundo plano.",
            CompletedAtUtc = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };

        _jobs[jobId] = failed;
        return true;
    }
}
