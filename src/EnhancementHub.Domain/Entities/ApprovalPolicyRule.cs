using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class ApprovalPolicyRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
    public RiskLevel? MinimumRiskLevel { get; set; }
    public string? Department { get; set; }
    public ApplicationTier? ApplicationTier { get; set; }
    public UserRole RequiredRole { get; set; } = UserRole.Approver;
    public bool BlockApproval { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}
