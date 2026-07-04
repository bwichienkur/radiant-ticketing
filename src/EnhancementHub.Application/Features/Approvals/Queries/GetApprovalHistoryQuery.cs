using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Approvals.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Approvals.Queries;

public sealed record GetApprovalHistoryQuery(Guid EnhancementRequestId)
    : IRequest<IReadOnlyList<ApprovalActionDto>>;

public sealed class GetApprovalHistoryQueryHandler
    : IRequestHandler<GetApprovalHistoryQuery, IReadOnlyList<ApprovalActionDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public GetApprovalHistoryQueryHandler(IEnhancementHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ApprovalActionDto>> Handle(
        GetApprovalHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var actions = await _dbContext.ApprovalActions
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.EnhancementRequestId == request.EnhancementRequestId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return actions.Select(a => a.ToDto()).ToList();
    }
}
