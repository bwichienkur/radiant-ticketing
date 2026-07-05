using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class IntakeCopilotSession : BaseEntity
{
    public Guid UserId { get; set; }
    public IntakeCopilotSessionStatus Status { get; set; } = IntakeCopilotSessionStatus.Active;
    public IntakeCopilotSource Source { get; set; } = IntakeCopilotSource.Web;
    public int TurnCount { get; set; }
    public string MessagesJson { get; set; } = "[]";
    public string? DraftJson { get; set; }
    public Guid? SuggestedTemplateId { get; set; }
    public Guid? CreatedRequestId { get; set; }
    public string? LastAssistantMessage { get; set; }

    public User? User { get; set; }
    public EnhancementTemplate? SuggestedTemplate { get; set; }
    public EnhancementRequest? CreatedRequest { get; set; }
}
