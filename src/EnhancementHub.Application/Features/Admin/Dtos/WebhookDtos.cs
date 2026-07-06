namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record WebhookSubscriptionSummaryDto(
    Guid Id,
    string Name,
    string Url,
    string SecretPrefix,
    string EventTypes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastDeliveryAt,
    int FailedDeliveryCount);

public sealed record CreateWebhookSubscriptionResultDto(
    Guid Id,
    string Name,
    string Secret,
    string SecretPrefix,
    string EventTypes);

public sealed record WebhookDeliverySummaryDto(
    Guid Id,
    Guid WebhookSubscriptionId,
    string SubscriptionName,
    string EventType,
    string Status,
    int AttemptCount,
    int? HttpStatusCode,
    string? LastError,
    DateTime CreatedAt,
    DateTime? DeliveredAt);
