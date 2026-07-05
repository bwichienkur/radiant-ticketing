using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantPlan Plan { get; set; } = TenantPlan.Trial;
    public TenantRegion Region { get; set; } = TenantRegion.US;
    public DateTime? TrialEndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BillingEmail { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<TenantUsageSnapshot> UsageSnapshots { get; set; } = new List<TenantUsageSnapshot>();
}
