using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class ScheduledRepositoryRefreshJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledRepositoryRefreshJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(12);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromDays(7);

    public ScheduledRepositoryRefreshJob(IServiceScopeFactory scopeFactory, ILogger<ScheduledRepositoryRefreshJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled repository refresh job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<IRepositoryIndexer>();
                await indexer.ReindexStaleRepositoriesAsync(_staleThreshold, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled repository refresh failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
