using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background;

public sealed class RepositoryIndexingJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RepositoryIndexingJob> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    public RepositoryIndexingJob(IServiceScopeFactory scopeFactory, ILogger<RepositoryIndexingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Repository indexing job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
                var indexer = scope.ServiceProvider.GetRequiredService<IRepositoryIndexer>();

                var pendingIds = await dbContext.Repositories
                    .Where(r => r.IndexingStatus == Domain.Enums.IndexingStatus.Pending)
                    .Select(r => r.Id)
                    .ToListAsync(stoppingToken);

                foreach (var id in pendingIds)
                {
                    await indexer.IndexRepositoryAsync(id, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Repository indexing job iteration failed.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
