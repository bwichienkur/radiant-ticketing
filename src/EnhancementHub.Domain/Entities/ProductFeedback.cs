using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class ProductFeedback : BaseEntity
{
    public Guid UserId { get; set; }
    public string WorkflowKey { get; set; } = string.Empty;
    public int NpsScore { get; set; }
    public string? Comment { get; set; }
    public Guid? TenantId { get; set; }

    public User User { get; set; } = null!;
}
