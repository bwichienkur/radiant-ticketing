using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class CompositeNotificationPublisher : INotificationPublisher
{
    private readonly IReadOnlyList<INotificationPublisher> _publishers;
    private readonly ILogger<CompositeNotificationPublisher> _logger;

    public CompositeNotificationPublisher(
        IEnumerable<INotificationPublisher> publishers,
        ILogger<CompositeNotificationPublisher> logger)
    {
        _publishers = publishers.ToList();
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var publisher in _publishers)
        {
            try
            {
                await publisher.PublishAsync(eventType, title, message, data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Notification publisher {Publisher} failed for event {EventType}",
                    publisher.GetType().Name,
                    eventType);
            }
        }
    }
}
