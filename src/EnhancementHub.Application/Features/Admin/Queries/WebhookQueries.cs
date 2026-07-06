using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Admin.Queries;

public sealed record ListWebhookSubscriptionsQuery : IRequest<IReadOnlyList<WebhookSubscriptionSummaryDto>>;

public sealed class ListWebhookSubscriptionsQueryHandler
    : IRequestHandler<ListWebhookSubscriptionsQuery, IReadOnlyList<WebhookSubscriptionSummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListWebhookSubscriptionsQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WebhookSubscriptionSummaryDto>> Handle(
        ListWebhookSubscriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var deliveryStats = await _dbContext.WebhookDeliveries
            .AsNoTracking()
            .GroupBy(d => d.WebhookSubscriptionId)
            .Select(g => new
            {
                SubscriptionId = g.Key,
                LastDeliveryAt = g.Max(d => d.DeliveredAt ?? d.CreatedAt),
                FailedCount = g.Count(d => d.Status == WebhookDeliveryStatus.Failed)
            })
            .ToDictionaryAsync(x => x.SubscriptionId, cancellationToken);

        return subscriptions.Select(subscription =>
        {
            deliveryStats.TryGetValue(subscription.Id, out var stats);
            return new WebhookSubscriptionSummaryDto(
                subscription.Id,
                subscription.Name,
                subscription.Url,
                subscription.SecretPrefix,
                subscription.EventTypes,
                subscription.IsActive,
                subscription.CreatedAt,
                stats?.LastDeliveryAt,
                stats?.FailedCount ?? 0);
        }).ToList();
    }
}

public sealed record ListWebhookDeliveriesQuery(int Limit = 50)
    : IRequest<IReadOnlyList<WebhookDeliverySummaryDto>>;

public sealed class ListWebhookDeliveriesQueryHandler
    : IRequestHandler<ListWebhookDeliveriesQuery, IReadOnlyList<WebhookDeliverySummaryDto>>
{
    private readonly IEnhancementHubDbContext _dbContext;

    public ListWebhookDeliveriesQueryHandler(IEnhancementHubDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<WebhookDeliverySummaryDto>> Handle(
        ListWebhookDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var deliveries = await _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Include(d => d.Subscription)
            .OrderByDescending(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return deliveries.Select(delivery => new WebhookDeliverySummaryDto(
            delivery.Id,
            delivery.WebhookSubscriptionId,
            delivery.Subscription.Name,
            delivery.EventType,
            delivery.Status.ToString(),
            delivery.AttemptCount,
            delivery.HttpStatusCode,
            delivery.LastError,
            delivery.CreatedAt,
            delivery.DeliveredAt)).ToList();
    }
}
