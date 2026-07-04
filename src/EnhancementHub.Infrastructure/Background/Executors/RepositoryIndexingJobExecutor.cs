using EnhancementHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class RepositoryIndexingJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RepositoryIndexingJobExecutor> _logger;

    public RepositoryIndexingJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<RepositoryIndexingJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<IRepositoryIndexer>();

        var pendingIds = await dbContext.Repositories
            .Where(r => r.IndexingStatus == Domain.Enums.IndexingStatus.Pending)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in pendingIds)
        {
            _logger.LogInformation("Indexing repository {RepositoryId}", id);
            await indexer.IndexRepositoryAsync(id, cancellationToken);
        }
    }
}
