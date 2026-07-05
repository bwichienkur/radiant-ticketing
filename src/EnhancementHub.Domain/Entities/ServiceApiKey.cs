using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class ServiceApiKey : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public Guid ServiceUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public User ServiceUser { get; set; } = null!;
}
