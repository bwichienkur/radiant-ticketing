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
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public TenantSubscriptionStatus SubscriptionStatus { get; set; } = TenantSubscriptionStatus.None;
    public DateTime? SubscriptionPeriodEnd { get; set; }
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.SharedRowLevel;
    public string? DatabaseSchemaName { get; set; }
    public DateTime? SchemaProvisionedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<TenantUsageSnapshot> UsageSnapshots { get; set; } = new List<TenantUsageSnapshot>();
    public TenantDeliveryProfile? DeliveryProfile { get; set; }
    public ICollection<TenantDeploymentEnvironment> DeploymentEnvironments { get; set; } = new List<TenantDeploymentEnvironment>();
}
