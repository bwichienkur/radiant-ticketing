using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;

    public User User { get; set; } = null!;
}
