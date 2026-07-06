using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class WebhookSubscription : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecretPrefix { get; set; } = string.Empty;
    public string SecretProtected { get; set; } = string.Empty;
    public string EventTypes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? TenantId { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
