using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid EnhancementRequestId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public EnhancementRequest EnhancementRequest { get; set; } = null!;
    public User User { get; set; } = null!;
}
