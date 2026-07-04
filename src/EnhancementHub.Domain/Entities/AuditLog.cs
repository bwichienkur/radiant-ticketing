using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Comments { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? AiModelUsed { get; set; }
    public string? PromptVersion { get; set; }
    public string? RetrievedContextReferences { get; set; }

    public User? User { get; set; }
}
