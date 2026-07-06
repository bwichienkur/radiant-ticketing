using EnhancementHub.Infrastructure.Services.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Background.Executors;

public sealed class WebhookDeliveryJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDeliveryJobExecutor> _logger;

    public WebhookDeliveryJobExecutor(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookDeliveryJobExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task DeliverAsync(Guid webhookDeliveryId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<WebhookDeliveryService>();
        try
        {
            await deliveryService.DeliverAsync(webhookDeliveryId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery job failed for {DeliveryId}", webhookDeliveryId);
            throw;
        }
    }
}
