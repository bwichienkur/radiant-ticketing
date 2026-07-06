using System.Text.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Webhooks;

public sealed class WebhookEventPublisher : IWebhookEventPublisher
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IWebhookDeliveryDispatcher _dispatcher;
    private readonly ILogger<WebhookEventPublisher> _logger;

    public WebhookEventPublisher(
        IEnhancementHubDbContext dbContext,
        IWebhookDeliveryDispatcher dispatcher,
        ILogger<WebhookEventPublisher> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        object payload,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Where(s => tenantId == null || s.TenantId == null || s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var matching = subscriptions
            .Where(subscription => WebhookEventTypes.Matches(subscription.EventTypes, eventType))
            .ToList();

        if (matching.Count == 0)
        {
            return;
        }

        var envelope = JsonSerializer.Serialize(new
        {
            eventType,
            timestamp = DateTime.UtcNow,
            data = payload
        });

        var now = DateTime.UtcNow;
        foreach (var subscription in matching)
        {
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = subscription.Id,
                EventType = eventType,
                PayloadJson = envelope,
                Status = WebhookDeliveryStatus.Pending,
                AttemptCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.WebhookDeliveries.Add(delivery);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                _dispatcher.EnqueueDelivery(delivery.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue webhook delivery {DeliveryId}", delivery.Id);
            }
        }
    }
}
