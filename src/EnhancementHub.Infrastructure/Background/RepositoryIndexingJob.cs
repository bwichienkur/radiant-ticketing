using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class RepositoryIndexingJob : BackgroundService
{
    private readonly RepositoryIndexingJobExecutor _executor;
    private readonly ILogger<RepositoryIndexingJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public RepositoryIndexingJob(
        RepositoryIndexingJobExecutor executor,
        ILogger<RepositoryIndexingJob> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Repository indexing job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Repository indexing job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
