using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Approvals.Dtos;

public sealed record ApprovalRecommendationDto(
    Guid EnhancementRequestId,
    string Recommendation,
    string Summary,
    RiskLevel? RiskLevel,
    double? ConfidenceScore,
    bool NeedsClarification);
