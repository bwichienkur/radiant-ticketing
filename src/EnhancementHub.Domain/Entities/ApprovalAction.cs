using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class ApprovalAction : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public Guid? EnhancementAnalysisId { get; set; }
    public Guid UserId { get; set; }
    public ApprovalActionType ActionType { get; set; }
    public string? Comments { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? AiModelUsed { get; set; }
    public string? PromptVersion { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public EnhancementAnalysis? EnhancementAnalysis { get; set; }
    public User User { get; set; } = null!;
}
