namespace EnhancementHub.Application.Abstractions;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string details,
        CancellationToken cancellationToken = default);
}
