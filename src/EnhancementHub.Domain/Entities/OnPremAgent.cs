using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class OnPremAgent : BaseEntity
{
    public Guid? ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Application? Application { get; set; }
}
