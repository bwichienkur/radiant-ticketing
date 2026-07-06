using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class DeliveryOrchestrationJob : BackgroundService
{
    private readonly DeliveryOrchestrationJobExecutor _executor;
    private readonly ILogger<DeliveryOrchestrationJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public DeliveryOrchestrationJob(DeliveryOrchestrationJobExecutor executor, ILogger<DeliveryOrchestrationJob> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Delivery orchestration job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Delivery orchestration iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
