using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Policies.Dtos;

public sealed record ApprovalPolicyRuleDto(
    Guid Id,
    string Name,
    bool IsEnabled,
    int Priority,
    RiskLevel? MinimumRiskLevel,
    string? Department,
    ApplicationTier? ApplicationTier,
    UserRole RequiredRole,
    bool BlockApproval,
    string Message,
    int? SlaTargetHours,
    bool EscalateOnBreach,
    UserRole? EscalateToRole);
