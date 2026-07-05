using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Options;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Services;

public sealed class IndexFreshnessService : IIndexFreshnessService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IndexingOptions _options;

    public IndexFreshnessService(
        IEnhancementHubDbContext dbContext,
        IOptions<IndexingOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<IndexFreshnessReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var slaHours = Math.Max(1, _options.FreshnessSlaHours);
        var cutoff = DateTime.UtcNow.AddHours(-slaHours);
        var now = DateTime.UtcNow;

        var repositories = await _dbContext.Repositories
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.ApplicationId,
                ApplicationName = r.Application.Name,
                r.LastIndexedAt,
                r.IndexingStatus
            })
            .ToListAsync(cancellationToken);

        var fresh = repositories.Count(r =>
            r.IndexingStatus == IndexingStatus.Completed
            && r.LastIndexedAt.HasValue
            && r.LastIndexedAt.Value >= cutoff);

        var neverIndexed = repositories.Count(r => r.LastIndexedAt is null);
        var inProgress = repositories.Count(r => r.IndexingStatus == IndexingStatus.InProgress);
        var failed = repositories.Count(r => r.IndexingStatus == IndexingStatus.Failed);
        var stale = repositories.Count(r =>
            r.IndexingStatus != IndexingStatus.InProgress
            && (r.LastIndexedAt is null || r.LastIndexedAt < cutoff));

        var total = repositories.Count;
        var freshnessPercent = total == 0 ? 100d : Math.Round(fresh * 100d / total, 1);
        var slaMet = total == 0 || freshnessPercent >= 95d;

        var staleRepositories = repositories
            .Where(r =>
                r.IndexingStatus != IndexingStatus.InProgress
                && (r.LastIndexedAt is null || r.LastIndexedAt < cutoff))
            .OrderBy(r => r.LastIndexedAt ?? DateTime.MinValue)
            .Take(25)
            .Select(r => new StaleRepositoryDto(
                r.Id,
                r.Name,
                r.ApplicationId,
                r.ApplicationName,
                r.LastIndexedAt,
                r.LastIndexedAt.HasValue
                    ? Math.Round((now - r.LastIndexedAt.Value).TotalHours, 1)
                    : null,
                r.IndexingStatus.ToString()))
            .ToList();

        return new IndexFreshnessReportDto(
            slaHours,
            total,
            fresh,
            stale,
            neverIndexed,
            inProgress,
            failed,
            freshnessPercent,
            slaMet,
            now,
            staleRepositories);
    }
}
