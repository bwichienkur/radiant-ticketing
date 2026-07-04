using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class EnhancementRequest : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? TargetApplicationId { get; set; }
    public DateTime? RequestedDueDate { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string? Department { get; set; }
    public Guid? TeamId { get; set; }
    public EnhancementRequestStatus Status { get; set; }
    public string? SupportingNotes { get; set; }

    public Application? TargetApplication { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public Team? Team { get; set; }
    public ICollection<EnhancementAttachment> Attachments { get; set; } = new List<EnhancementAttachment>();
    public ICollection<EnhancementAnalysis> Analyses { get; set; } = new List<EnhancementAnalysis>();
    public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ExternalTicket> ExternalTickets { get; set; } = new List<ExternalTicket>();
    public ICollection<AiPromptRun> AiPromptRuns { get; set; } = new List<AiPromptRun>();
    public ICollection<RefactorPlan> RefactorPlans { get; set; } = new List<RefactorPlan>();
}
