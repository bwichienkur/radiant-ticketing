using EnhancementHub.Infrastructure.Background.Executors;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class HangfireRepositoryIndexingDispatcher
{
    private readonly ILogger<HangfireRepositoryIndexingDispatcher> _logger;

    public HangfireRepositoryIndexingDispatcher(ILogger<HangfireRepositoryIndexingDispatcher> logger) =>
        _logger = logger;

    public Task DispatchAsync(IReadOnlyList<Guid> repositoryIds, CancellationToken cancellationToken = default)
    {
        foreach (var repositoryId in repositoryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BackgroundJob.Enqueue<RepositoryIndexingJobExecutor>(
                executor => executor.IndexSingleRepositoryAsync(repositoryId, CancellationToken.None));
            _logger.LogInformation("Enqueued Hangfire indexing job for repository {RepositoryId}", repositoryId);
        }

        return Task.CompletedTask;
    }
}
