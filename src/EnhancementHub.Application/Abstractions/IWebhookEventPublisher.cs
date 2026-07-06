namespace EnhancementHub.Application.Abstractions;

public interface IWebhookEventPublisher
{
    Task PublishAsync(
        string eventType,
        object payload,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}

public interface IWebhookDeliveryDispatcher
{
    void EnqueueDelivery(Guid webhookDeliveryId);
}
