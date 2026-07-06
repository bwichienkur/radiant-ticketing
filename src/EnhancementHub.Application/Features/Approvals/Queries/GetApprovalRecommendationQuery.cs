using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Approvals.Dtos;
using MediatR;

namespace EnhancementHub.Application.Features.Approvals.Queries;

public sealed record GetApprovalRecommendationQuery(Guid EnhancementRequestId)
    : IRequest<ApprovalRecommendationDto>;

public sealed class GetApprovalRecommendationQueryHandler
    : IRequestHandler<GetApprovalRecommendationQuery, ApprovalRecommendationDto>
{
    private readonly IApprovalCopilotService _approvalCopilot;

    public GetApprovalRecommendationQueryHandler(IApprovalCopilotService approvalCopilot) =>
        _approvalCopilot = approvalCopilot;

    public Task<ApprovalRecommendationDto> Handle(
        GetApprovalRecommendationQuery request,
        CancellationToken cancellationToken) =>
        _approvalCopilot.GetRecommendationAsync(request.EnhancementRequestId, cancellationToken);
}
