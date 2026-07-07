using EnhancementHub.Application.Features.Approvals.Dtos;

namespace EnhancementHub.Application.Abstractions;

public interface IApprovalCopilotService
{
    Task<ApprovalRecommendationDto> GetRecommendationAsync(
        Guid enhancementRequestId,
        CancellationToken cancellationToken = default);
}
