using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Enums;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class BackgroundJobStatusService : IBackgroundJobStatusService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public BackgroundJobStatusService(
        IEnhancementHubDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<BackgroundJobsStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var provider = _configuration["BackgroundJobs:Provider"] ?? "Polling";

        var queueCounts = new BackgroundJobQueueCountsDto(
            await _dbContext.Repositories.CountAsync(
                r => r.IndexingStatus == IndexingStatus.Pending, cancellationToken),
            await _dbContext.EnhancementRequests.CountAsync(
                r => r.Status == EnhancementRequestStatus.Submitted
                    || r.Status == EnhancementRequestStatus.AiAnalyzing,
                cancellationToken),
            await _dbContext.OnboardingSessions.CountAsync(
                s => s.DiscoveryJobState == DiscoveryJobState.Queued, cancellationToken),
            await _dbContext.DatabaseConnections.CountAsync(
                c => c.ScanStatus == "Pending", cancellationToken));

        var jobs = BuildJobDefinitions(provider);
        var hangfire = TryGetHangfireStats();
        var failedJobs = TryGetFailedJobs();

        return new BackgroundJobsStatusDto(
            provider,
            DateTime.UtcNow,
            queueCounts,
            jobs,
            hangfire,
            failedJobs);
    }

    public Task<bool> RetryFailedJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Task.FromResult(false);
        }

        var provider = _configuration["BackgroundJobs:Provider"] ?? "Polling";
        if (!provider.Equals("Hangfire", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        try
        {
            var monitoring = JobStorage.Current?.GetMonitoringApi();
            if (monitoring is null)
            {
                return Task.FromResult(false);
            }

            var failedJob = monitoring.FailedJobs(0, 100).FirstOrDefault(j => j.Key == jobId);
            if (failedJob.Key is null)
            {
                return Task.FromResult(false);
            }

            BackgroundJob.Requeue(jobId);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static IReadOnlyList<BackgroundJobDefinitionStatusDto> BuildJobDefinitions(string provider)
    {
        if (!provider.Equals("Hangfire", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new("repository-indexing", "Index pending repositories", "Polling every 5 minutes", null, null),
                new("ai-analysis", "Analyze submitted enhancement requests", "Polling every 2 minutes", null, null),
                new("application-discovery", "Run onboarding discovery jobs", "Polling every 3 seconds", null, null),
                new("database-schema-scan", "Scan pending database connections", "Polling every 10 minutes", null, null),
                new("repository-refresh", "Re-index stale repositories", "Polling every 12 hours", null, null),
                new("data-retention", "Purge expired AI prompt runs and attachments", "Polling every 24 hours", null, null)
            ];
        }

        try
        {
            var connection = JobStorage.Current?.GetConnection();
            if (connection is null)
            {
                return BuildHangfireJobsWithoutStorage();
            }

            return
            [
                BuildRecurringJobStatus(connection, "repository-indexing", "Index pending repositories", "*/5 * * * *"),
                BuildRecurringJobStatus(connection, "ai-analysis", "Analyze submitted enhancement requests", "*/2 * * * *"),
                BuildRecurringJobStatus(connection, "application-discovery", "Run onboarding discovery jobs", "* * * * *"),
                BuildRecurringJobStatus(connection, "database-schema-scan", "Scan pending database connections", "*/10 * * * *"),
                BuildRecurringJobStatus(connection, "repository-refresh", "Re-index stale repositories", "Daily"),
                BuildRecurringJobStatus(connection, "data-retention", "Purge expired AI prompt runs and attachments", "Daily")
            ];
        }
        catch
        {
            return BuildHangfireJobsWithoutStorage();
        }
    }

    private static IReadOnlyList<BackgroundJobDefinitionStatusDto> BuildHangfireJobsWithoutStorage() =>
    [
        new("repository-indexing", "Index pending repositories", "*/5 * * * *", null, null),
        new("ai-analysis", "Analyze submitted enhancement requests", "*/2 * * * *", null, null),
        new("application-discovery", "Run onboarding discovery jobs", "* * * * *", null, null),
        new("database-schema-scan", "Scan pending database connections", "*/10 * * * *", null, null),
        new("repository-refresh", "Re-index stale repositories", "Daily", null, null),
        new("data-retention", "Purge expired AI prompt runs and attachments", "Daily", null, null)
    ];

    private static BackgroundJobDefinitionStatusDto BuildRecurringJobStatus(
        IStorageConnection connection,
        string jobId,
        string description,
        string schedule)
    {
        var recurring = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);

        return new BackgroundJobDefinitionStatusDto(
            jobId,
            description,
            schedule,
            recurring?.LastExecution?.ToString("u"),
            recurring?.NextExecution?.ToString("u"));
    }

    private static BackgroundJobHangfireStatsDto? TryGetHangfireStats()
    {
        try
        {
            var monitoring = JobStorage.Current?.GetMonitoringApi();
            if (monitoring is null)
            {
                return null;
            }

            StatisticsDto stats = monitoring.GetStatistics();
            return new BackgroundJobHangfireStatsDto(
                stats.Enqueued,
                stats.Processing,
                stats.Scheduled,
                stats.Succeeded,
                stats.Failed,
                stats.Deleted);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<BackgroundJobFailedJobDto> TryGetFailedJobs()
    {
        try
        {
            var monitoring = JobStorage.Current?.GetMonitoringApi();
            if (monitoring is null)
            {
                return [];
            }

            return monitoring.FailedJobs(0, 20)
                .Select(job => new BackgroundJobFailedJobDto(
                    job.Key,
                    job.Value.Job?.ToString(),
                    job.Value.FailedAt?.ToString("u"),
                    Truncate(job.Value.ExceptionMessage, 500)))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength] + "…";
}
