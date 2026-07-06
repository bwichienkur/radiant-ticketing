using EnhancementHub.Domain.Common;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Domain.Entities;

public class WebhookDelivery : BaseEntity
{
    public Guid WebhookSubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? LastError { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public WebhookSubscription Subscription { get; set; } = null!;
}
