using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed record GetPortfolioHealthQuery : IRequest<PortfolioHealthReportDto>;

public sealed class GetPortfolioHealthQueryHandler
    : IRequestHandler<GetPortfolioHealthQuery, PortfolioHealthReportDto>
{
    private readonly IReportingDbContext _dbContext;
    private readonly IIndexFreshnessService _indexFreshness;

    public GetPortfolioHealthQueryHandler(
        IReportingDbContext dbContext,
        IIndexFreshnessService indexFreshness)
    {
        _dbContext = dbContext;
        _indexFreshness = indexFreshness;
    }

    public async Task<PortfolioHealthReportDto> Handle(
        GetPortfolioHealthQuery request,
        CancellationToken cancellationToken)
    {
        var applications = await _dbContext.Applications
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(cancellationToken);

        var driftByApp = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Where(f => !f.IsResolved)
            .GroupBy(f => f.DatabaseConnection.ApplicationId)
            .Select(g => new { ApplicationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Count, cancellationToken);

        var pendingByApp = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.Status == EnhancementRequestStatus.PendingApproval)
            .Where(r => r.TargetApplicationId != null)
            .GroupBy(r => r.TargetApplicationId!.Value)
            .Select(g => new { ApplicationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Count, cancellationToken);

        var highRiskByApp = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => a.RiskLevel == RiskLevel.High || a.RiskLevel == RiskLevel.Critical)
            .Join(
                _dbContext.EnhancementRequests.AsNoTracking()
                    .Where(r => r.Status == EnhancementRequestStatus.PendingApproval && r.TargetApplicationId != null),
                analysis => analysis.EnhancementRequestId,
                enhancementRequest => enhancementRequest.Id,
                (analysis, enhancementRequest) => enhancementRequest.TargetApplicationId!.Value)
            .GroupBy(applicationId => applicationId)
            .Select(g => new { ApplicationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ApplicationId, x => x.Count, cancellationToken);

        var freshness = await _indexFreshness.GetReportAsync(cancellationToken);
        var staleByApp = freshness.StaleRepositories
            .GroupBy(r => r.ApplicationId)
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = applications
            .Select(app =>
            {
                var drift = driftByApp.GetValueOrDefault(app.Id);
                var pending = pendingByApp.GetValueOrDefault(app.Id);
                var highRisk = highRiskByApp.GetValueOrDefault(app.Id);
                var stale = staleByApp.GetValueOrDefault(app.Id);
                var score = Math.Min(
                    100,
                    drift * 12 + pending * 8 + highRisk * 15 + stale * 10);

                return new PortfolioApplicationHealthDto(
                    app.Id,
                    app.Name,
                    drift,
                    pending,
                    highRisk,
                    stale,
                    score);
            })
            .OrderByDescending(r => r.RiskScore)
            .ThenBy(r => r.ApplicationName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PortfolioHealthReportDto(rows, DateTime.UtcNow);
    }
}
