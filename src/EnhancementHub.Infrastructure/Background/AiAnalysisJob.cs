using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class AiAnalysisJob : BackgroundService
{
    private readonly AiAnalysisJobExecutor _executor;
    private readonly ILogger<AiAnalysisJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(2);

    public AiAnalysisJob(AiAnalysisJobExecutor executor, ILogger<AiAnalysisJob> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI analysis job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "AI analysis job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
