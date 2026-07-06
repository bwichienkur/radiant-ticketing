using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Infrastructure.Persistence.Repositories;

public sealed class EnhancementAnalysisRepository : IEnhancementAnalysisRepository
{
    private readonly EnhancementHubDbContext _dbContext;

    public EnhancementAnalysisRepository(EnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<EnhancementAnalysis?> GetByRequestAsync(
        Guid enhancementRequestId,
        int? version,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Include(a => a.Findings)
            .Include(a => a.AffectedApplications).ThenInclude(a => a.Application)
            .Include(a => a.AffectedRepositories).ThenInclude(r => r.Repository)
            .Include(a => a.AffectedComponents)
            .Include(a => a.DatabaseChangeRecommendations)
            .Include(a => a.ApiChangeRecommendations)
            .Include(a => a.RiskAssessments)
            .Where(a => a.EnhancementRequestId == enhancementRequestId);

        if (version.HasValue)
        {
            return await query.FirstOrDefaultAsync(a => a.Version == version.Value, cancellationToken);
        }

        return await query.OrderByDescending(a => a.Version).FirstOrDefaultAsync(cancellationToken);
    }
}
