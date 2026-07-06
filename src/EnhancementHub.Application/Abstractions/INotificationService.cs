using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface INotificationService
{
    Task<NotificationDto> NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? entityType = null,
        Guid? entityId = null,
        string? actionUrl = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task NotifyApproversOfPendingApprovalAsync(
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task NotifySubmitterOfAnalysisCompleteAsync(
        Guid submitterUserId,
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task NotifyAdminsOfCriticalDriftAsync(
        Guid databaseConnectionId,
        string connectionName,
        int criticalCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task NotifyArchitectsOfDriftDigestAsync(
        int unresolvedCount,
        IReadOnlyList<DriftDigestFindingSummary> topFindings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> ListForUserAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationPreferenceDto>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UpdatePreferencesAsync(
        Guid userId,
        IReadOnlyList<UpdateNotificationPreferenceInput> preferences,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime? ReadAt,
    string? EntityType,
    Guid? EntityId,
    string? ActionUrl,
    DateTime CreatedAt);

public sealed record NotificationPreferenceDto(
    NotificationType Type,
    string Label,
    bool EmailEnabled,
    bool InAppEnabled);

public sealed record UpdateNotificationPreferenceInput(
    NotificationType Type,
    bool EmailEnabled,
    bool InAppEnabled);

public sealed record DriftDigestFindingSummary(
    string Title,
    string Severity,
    string ConnectionName,
    Guid DatabaseConnectionId);
