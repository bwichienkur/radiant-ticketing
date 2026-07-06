using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnhancementHub.Web.Services;

public sealed class SignalRUserNotificationNotifier : IUserNotificationNotifier
{
    private readonly IHubContext<PlatformNotificationHub> _hubContext;

    public SignalRUserNotificationNotifier(IHubContext<PlatformNotificationHub> hubContext) =>
        _hubContext = hubContext;

    public Task NotifyUserAsync(
        Guid userId,
        UserNotificationPayload notification,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(NotificationGroupNames.ForUser(userId))
            .SendAsync("UserNotification", notification, cancellationToken);
}
