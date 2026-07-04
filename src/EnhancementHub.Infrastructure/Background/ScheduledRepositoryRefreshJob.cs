using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class ScheduledRepositoryRefreshJob : BackgroundService
{
    private readonly ScheduledRepositoryRefreshJobExecutor _executor;
    private readonly ILogger<ScheduledRepositoryRefreshJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(12);

    public ScheduledRepositoryRefreshJob(
        ScheduledRepositoryRefreshJobExecutor executor,
        ILogger<ScheduledRepositoryRefreshJob> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled repository refresh job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled repository refresh failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
