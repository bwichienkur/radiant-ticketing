using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class RepositoryIndexingJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RepositoryIndexingJobExecutor> _logger;

    public RepositoryIndexingJobExecutor(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RepositoryIndexingJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IEnhancementHubDbContext>();

        var pendingIds = await dbContext.Repositories
            .Where(r => r.IndexingStatus == IndexingStatus.Pending)
            .OrderByDescending(r => r.IndexingPriority)
            .ThenBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (pendingIds.Count == 0)
        {
            return;
        }

        if (ShouldShardViaHangfire())
        {
            var hangfireDispatcher = scope.ServiceProvider.GetRequiredService<HangfireRepositoryIndexingDispatcher>();
            await hangfireDispatcher.DispatchAsync(pendingIds, cancellationToken);
            return;
        }

        foreach (var repositoryId in pendingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await IndexSingleRepositoryAsync(repositoryId, cancellationToken);
        }
    }

    [Queue("indexing")]
    public async Task IndexSingleRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IRepositoryIndexer>();
        _logger.LogInformation("Indexing repository {RepositoryId}", repositoryId);
        await indexer.IndexRepositoryAsync(repositoryId, cancellationToken);
    }

    private bool ShouldShardViaHangfire()
    {
        var provider = _configuration["BackgroundJobs:Provider"] ?? "Polling";
        var shardingEnabled = _configuration.GetValue("Indexing:ShardJobsPerRepository", true);
        return shardingEnabled
            && provider.Equals("Hangfire", StringComparison.OrdinalIgnoreCase);
    }
}
