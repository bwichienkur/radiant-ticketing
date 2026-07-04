using EnhancementHub.Infrastructure.Background.Executors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EnhancementHub.Infrastructure.Options;

namespace EnhancementHub.Infrastructure.Background;

public sealed class DataRetentionJob : BackgroundService
{
    private readonly DataRetentionJobExecutor _executor;
    private readonly RetentionOptions _options;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(
        DataRetentionJobExecutor executor,
        IOptions<RetentionOptions> options,
        ILogger<DataRetentionJob> logger)
    {
        _executor = executor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data retention job started (polling mode).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _executor.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Data retention job iteration failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
