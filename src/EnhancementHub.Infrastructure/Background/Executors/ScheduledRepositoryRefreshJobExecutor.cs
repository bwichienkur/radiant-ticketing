using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class ScheduledRepositoryRefreshJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledRepositoryRefreshJobExecutor> _logger;
    private readonly TimeSpan _staleThreshold = TimeSpan.FromDays(7);

    public ScheduledRepositoryRefreshJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledRepositoryRefreshJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IRepositoryIndexer>();
        _logger.LogInformation("Refreshing stale repositories.");
        await indexer.ReindexStaleRepositoriesAsync(_staleThreshold, cancellationToken);
    }
}
