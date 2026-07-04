using EnhancementHub.Application.Abstractions;

namespace EnhancementHub.Infrastructure.Services;

public sealed class NoOpNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync(string eventType, string title, string message, object? data = null, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
