using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Queries;

public enum EnhancementRequestSort
{
    Newest,
    Oldest,
    HighestRisk,
    Priority
}

public sealed record ListEnhancementRequestsQuery(
    EnhancementRequestStatus? Status = null,
    Guid? TargetApplicationId = null,
    string? Search = null,
    string? Priority = null,
    RiskLevel? MinRisk = null,
    EnhancementRequestSort Sort = EnhancementRequestSort.Newest) : IRequest<IReadOnlyList<EnhancementRequestDto>>;

public sealed class ListEnhancementRequestsQueryHandler
    : IRequestHandler<ListEnhancementRequestsQuery, IReadOnlyList<EnhancementRequestDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IEnhancementRequestAccessService _accessService;

    public ListEnhancementRequestsQueryHandler(
        IEnhancementHubDbContext dbContext,
        IEnhancementRequestAccessService accessService)
    {
        _dbContext = dbContext;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<EnhancementRequestDto>> Handle(
        ListEnhancementRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _accessService.ApplyVisibilityFilter(
                _dbContext.EnhancementRequests
                    .AsNoTracking()
                    .Include(r => r.TargetApplication)
                    .Include(r => r.SubmittedByUser))
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.TargetApplicationId.HasValue)
        {
            query = query.Where(r => r.TargetApplicationId == request.TargetApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Title.ToLower().Contains(term)
                || r.BusinessDescription.ToLower().Contains(term)
                || (r.SubmittedByUser != null && r.SubmittedByUser.DisplayName.ToLower().Contains(term))
                || (r.TargetApplication != null && r.TargetApplication.Name.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            query = query.Where(r => r.Priority == request.Priority);
        }

        var entities = await query.ToListAsync(cancellationToken);

        var requestIds = entities.Select(e => e.Id).ToList();
        var latestRisks = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => requestIds.Contains(a.EnhancementRequestId))
            .GroupBy(a => a.EnhancementRequestId)
            .Select(g => new
            {
                RequestId = g.Key,
                Risk = g.OrderByDescending(a => a.Version).First().RiskLevel
            })
            .ToDictionaryAsync(x => x.RequestId, x => x.Risk, cancellationToken);

        var now = DateTime.UtcNow;
        var results = entities.Select(e =>
        {
            latestRisks.TryGetValue(e.Id, out var risk);
            var daysInStatus = (int)Math.Floor((now - e.UpdatedAt).TotalDays);
            var dto = e.ToDto();
            return dto with { LatestRiskLevel = risk, DaysInStatus = daysInStatus };
        }).ToList();

        if (request.MinRisk.HasValue)
        {
            results = results
                .Where(r => r.LatestRiskLevel.HasValue && r.LatestRiskLevel.Value >= request.MinRisk.Value)
                .ToList();
        }

        results = request.Sort switch
        {
            EnhancementRequestSort.Oldest => results.OrderBy(r => r.CreatedAt).ToList(),
            EnhancementRequestSort.HighestRisk => results
                .OrderByDescending(r => r.LatestRiskLevel ?? RiskLevel.Low)
                .ThenByDescending(r => r.CreatedAt)
                .ToList(),
            EnhancementRequestSort.Priority => results
                .OrderByDescending(r => r.Priority switch
                {
                    "Critical" => 4,
                    "High" => 3,
                    "Medium" => 2,
                    _ => 1
                })
                .ThenByDescending(r => r.CreatedAt)
                .ToList(),
            _ => results.OrderByDescending(r => r.CreatedAt).ToList()
        };

        return results;
    }
}
