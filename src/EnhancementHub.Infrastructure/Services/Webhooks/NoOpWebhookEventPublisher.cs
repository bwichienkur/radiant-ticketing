using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Services.Webhooks;

public sealed class NoOpWebhookEventPublisher : IWebhookEventPublisher
{
    public Task PublishAsync(
        string eventType,
        object payload,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
