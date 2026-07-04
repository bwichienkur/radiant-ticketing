using EnhancementHub.Application.Abstractions;
using EnhancementHub.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EnhancementHub.Web.Services;

public sealed class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<PlatformNotificationHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<PlatformNotificationHub> hubContext) =>
        _hubContext = hubContext;

    public async Task PublishAsync(
        string eventType,
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync(
            "PlatformNotification",
            new
            {
                eventType,
                title,
                message,
                data,
                timestamp = DateTime.UtcNow
            },
            cancellationToken);
    }
}
