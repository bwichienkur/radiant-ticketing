using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Mappings;
using EnhancementHub.Application.Features.Approvals.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Approvals.Queries;

public sealed record GetApprovalHistoryQuery(Guid EnhancementRequestId)
    : IRequest<IReadOnlyList<ApprovalActionDto>>;

public sealed class GetApprovalHistoryQueryHandler
    : IRequestHandler<GetApprovalHistoryQuery, IReadOnlyList<ApprovalActionDto>>
{
    private readonly IEnhancementRequestRepository _requests;

    public GetApprovalHistoryQueryHandler(IEnhancementRequestRepository requests) => _requests = requests;

    public async Task<IReadOnlyList<ApprovalActionDto>> Handle(
        GetApprovalHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var actions = await _requests.ListApprovalActionsAsync(request.EnhancementRequestId, cancellationToken);
        return actions.Select(a => a.ToDto()).ToList();
    }
}
