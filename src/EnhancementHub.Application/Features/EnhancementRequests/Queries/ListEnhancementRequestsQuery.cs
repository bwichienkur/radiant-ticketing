using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.EnhancementRequests.Queries;

public sealed record ListEnhancementRequestsQuery(
    EnhancementRequestStatus? Status = null,
    Guid? TargetApplicationId = null) : IRequest<IReadOnlyList<EnhancementRequestDto>>;

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

        var entities = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDto()).ToList();
    }
}
