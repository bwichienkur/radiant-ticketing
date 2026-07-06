using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? ActionUrl { get; set; }
    public Guid? TenantId { get; set; }

    public User User { get; set; } = null!;
}
