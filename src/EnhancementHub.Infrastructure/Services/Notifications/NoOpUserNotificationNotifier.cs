using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class NoOpUserNotificationNotifier : IUserNotificationNotifier
{
    public Task NotifyUserAsync(
        Guid userId,
        UserNotificationPayload notification,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
