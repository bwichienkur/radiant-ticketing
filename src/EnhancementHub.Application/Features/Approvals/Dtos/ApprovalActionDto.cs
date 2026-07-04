using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Approvals.Dtos;

public sealed record ApprovalActionDto(
    Guid Id,
    Guid EnhancementRequestId,
    Guid? EnhancementAnalysisId,
    Guid UserId,
    string UserDisplayName,
    ApprovalActionType ActionType,
    string? Comments,
    string? PreviousValue,
    string? NewValue,
    DateTime CreatedAt);
