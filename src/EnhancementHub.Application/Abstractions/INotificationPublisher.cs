namespace EnhancementHub.Application.Abstractions;

public interface INotificationPublisher
{
    Task PublishAsync(string eventType, string title, string message, object? data = null, CancellationToken cancellationToken = default);
}
