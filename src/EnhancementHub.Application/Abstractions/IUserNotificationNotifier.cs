using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IUserNotificationNotifier
{
    Task NotifyUserAsync(
        Guid userId,
        UserNotificationPayload notification,
        CancellationToken cancellationToken = default);
}

public sealed record UserNotificationPayload(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? EntityType,
    Guid? EntityId,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAt);
