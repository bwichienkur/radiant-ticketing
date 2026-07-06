using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Background.Executors;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Webhooks;

public sealed class WebhookDeliveryDispatcher : IWebhookDeliveryDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDeliveryDispatcher> _logger;

    public WebhookDeliveryDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookDeliveryDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void EnqueueDelivery(Guid webhookDeliveryId)
    {
        try
        {
            BackgroundJob.Enqueue<WebhookDeliveryJobExecutor>(executor =>
                executor.DeliverAsync(webhookDeliveryId, CancellationToken.None));
        }
        catch (InvalidOperationException)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var deliveryService = scope.ServiceProvider.GetRequiredService<WebhookDeliveryService>();
                    await deliveryService.DeliverAsync(webhookDeliveryId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Inline webhook delivery failed for {DeliveryId}", webhookDeliveryId);
                }
            });
        }
    }
}

public sealed class NoOpWebhookDeliveryDispatcher : IWebhookDeliveryDispatcher
{
    public void EnqueueDelivery(Guid webhookDeliveryId)
    {
    }
}
