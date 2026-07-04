using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.EnhancementRequests.Dtos;

public sealed record EnhancementRequestDto(
    Guid Id,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    string? TargetApplicationName,
    DateTime? RequestedDueDate,
    Guid SubmittedByUserId,
    string? SubmittedByUserName,
    string? Department,
    Guid? TeamId,
    EnhancementRequestStatus Status,
    string? SupportingNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record EnhancementAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    DateTime CreatedAt);

public sealed record CommentSummaryDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string Content,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record EnhancementRequestDetailDto(
    Guid Id,
    string Title,
    string BusinessDescription,
    string DesiredOutcome,
    string Priority,
    Guid? TargetApplicationId,
    string? TargetApplicationName,
    DateTime? RequestedDueDate,
    Guid SubmittedByUserId,
    string SubmittedByUserName,
    string? Department,
    Guid? TeamId,
    EnhancementRequestStatus Status,
    string? SupportingNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<EnhancementAttachmentDto> Attachments,
    IReadOnlyList<CommentSummaryDto> Comments,
    IReadOnlyList<EnhancementAnalysisSummaryDto> Analyses);

public sealed record EnhancementAnalysisSummaryDto(
    Guid Id,
    int Version,
    string? FeatureSummary,
    RiskLevel RiskLevel,
    double ConfidenceScore,
    bool NeedsClarification,
    bool IsApprovedSnapshot,
    DateTime CreatedAt);
