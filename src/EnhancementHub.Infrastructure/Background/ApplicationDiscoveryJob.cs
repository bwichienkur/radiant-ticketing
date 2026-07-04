using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class ApplicationDiscoveryJob : BackgroundService
{
    private readonly ApplicationDiscoveryJobExecutor _executor;
    private readonly ILogger<ApplicationDiscoveryJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(3);

    public ApplicationDiscoveryJob(
        ApplicationDiscoveryJobExecutor executor,
        ILogger<ApplicationDiscoveryJob> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Application discovery job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Application discovery job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
