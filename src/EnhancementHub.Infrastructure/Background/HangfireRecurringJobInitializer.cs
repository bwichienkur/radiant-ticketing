using EnhancementHub.Infrastructure.Background.Executors;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class HangfireRecurringJobInitializer : IHostedService
{
    private readonly ILogger<HangfireRecurringJobInitializer> _logger;

    public HangfireRecurringJobInitializer(ILogger<HangfireRecurringJobInitializer> logger) =>
        _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RecurringJob.AddOrUpdate<RepositoryIndexingJobExecutor>(
            "repository-indexing",
            executor => executor.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<AiAnalysisJobExecutor>(
            "ai-analysis",
            executor => executor.ExecuteAsync(CancellationToken.None),
            "*/2 * * * *");

        RecurringJob.AddOrUpdate<ApplicationDiscoveryJobExecutor>(
            "application-discovery",
            executor => executor.ExecuteAsync(CancellationToken.None),
            "* * * * *");

        RecurringJob.AddOrUpdate<DatabaseSchemaScanJobExecutor>(
            "database-schema-scan",
            executor => executor.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *");

        RecurringJob.AddOrUpdate<ScheduledRepositoryRefreshJobExecutor>(
            "repository-refresh",
            executor => executor.ExecuteAsync(CancellationToken.None),
            Cron.Daily);

        _logger.LogInformation("Hangfire recurring jobs registered.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
