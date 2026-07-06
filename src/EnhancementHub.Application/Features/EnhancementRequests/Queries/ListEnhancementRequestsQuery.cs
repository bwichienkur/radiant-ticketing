using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Entities;
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
    EnhancementRequestSort Sort = EnhancementRequestSort.Newest,
    IReadOnlyList<Guid>? Ids = null,
    int Page = 1,
    int PageSize = 0) : IRequest<PagedResult<EnhancementRequestDto>>;

public sealed class ListEnhancementRequestsQueryHandler
    : IRequestHandler<ListEnhancementRequestsQuery, PagedResult<EnhancementRequestDto>>
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

    public async Task<PagedResult<EnhancementRequestDto>> Handle(
        ListEnhancementRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var baseQuery = _accessService.ApplyVisibilityFilter(
                _dbContext.EnhancementRequests.AsNoTracking())
            .AsQueryable();

        if (request.Status.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.Status == request.Status.Value);
        }

        if (request.TargetApplicationId.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.TargetApplicationId == request.TargetApplicationId.Value);
        }

        if (request.Ids is { Count: > 0 })
        {
            baseQuery = baseQuery.Where(r => request.Ids.Contains(r.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(r =>
                r.Title.ToLower().Contains(term)
                || r.BusinessDescription.ToLower().Contains(term)
                || (r.SubmittedByUser != null && r.SubmittedByUser.DisplayName.ToLower().Contains(term))
                || (r.TargetApplication != null && r.TargetApplication.Name.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            baseQuery = baseQuery.Where(r => r.Priority == request.Priority);
        }

        var projected = baseQuery.Select(r => new RequestProjection
        {
            RequestId = r.Id,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Priority = r.Priority,
            LatestRisk = _dbContext.EnhancementAnalyses
                .Where(a => a.EnhancementRequestId == r.Id)
                .OrderByDescending(a => a.Version)
                .Select(a => (RiskLevel?)a.RiskLevel)
                .FirstOrDefault(),
        });

        if (request.MinRisk.HasValue)
        {
            projected = projected.Where(x =>
                x.LatestRisk.HasValue && x.LatestRisk.Value >= request.MinRisk.Value);
        }

        projected = request.Sort switch
        {
            EnhancementRequestSort.Oldest => projected.OrderBy(x => x.CreatedAt),
            EnhancementRequestSort.HighestRisk => projected
                .OrderByDescending(x => x.LatestRisk ?? RiskLevel.Low)
                .ThenByDescending(x => x.CreatedAt),
            EnhancementRequestSort.Priority => projected
                .OrderByDescending(x => x.Priority == "Critical" ? 4
                    : x.Priority == "High" ? 3
                    : x.Priority == "Medium" ? 2
                    : 1)
                .ThenByDescending(x => x.CreatedAt),
            _ => projected.OrderByDescending(x => x.CreatedAt),
        };

        var totalCount = await projected.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize;

        IQueryable<RequestProjection> pageQuery = projected;
        if (pageSize > 0)
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            pageQuery = projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }
        else
        {
            page = 1;
            pageSize = totalCount;
        }

        var rows = await pageQuery.ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return new PagedResult<EnhancementRequestDto>([], totalCount, page, pageSize);
        }

        var pageIds = rows.Select(r => r.RequestId).ToList();
        var entities = await _accessService.ApplyVisibilityFilter(
                _dbContext.EnhancementRequests
                    .AsNoTracking()
                    .Include(r => r.TargetApplication)
                    .Include(r => r.SubmittedByUser))
            .Where(r => pageIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var results = rows
            .Where(row => entities.ContainsKey(row.RequestId))
            .Select(row =>
            {
                var entity = entities[row.RequestId];
                var daysInStatus = (int)Math.Floor((now - entity.UpdatedAt).TotalDays);
                var dto = entity.ToDto();
                return dto with { LatestRiskLevel = row.LatestRisk, DaysInStatus = daysInStatus };
            })
            .ToList();

        return new PagedResult<EnhancementRequestDto>(results, totalCount, page, pageSize);
    }

    private sealed class RequestProjection
    {
        public Guid RequestId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string Priority { get; init; } = string.Empty;
        public RiskLevel? LatestRisk { get; init; }
    }
}
