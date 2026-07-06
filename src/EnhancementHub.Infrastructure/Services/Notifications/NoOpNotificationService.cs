using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class NoOpNotificationService : INotificationService
{
    public Task<NotificationDto> NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? entityType = null,
        Guid? entityId = null,
        string? actionUrl = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationDto(
            Guid.Empty,
            type,
            title,
            message,
            true,
            null,
            entityType,
            entityId,
            actionUrl,
            DateTime.UtcNow));

    public Task NotifyApproversOfPendingApprovalAsync(
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifySubmitterOfAnalysisCompleteAsync(
        Guid submitterUserId,
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyAdminsOfCriticalDriftAsync(
        Guid databaseConnectionId,
        string connectionName,
        int criticalCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyArchitectsOfDriftDigestAsync(
        int unresolvedCount,
        IReadOnlyList<DriftDigestFindingSummary> topFindings,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<NotificationDto>> ListForUserAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>([]);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<NotificationPreferenceDto>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationPreferenceDto>>([]);

    public Task UpdatePreferencesAsync(
        Guid userId,
        IReadOnlyList<UpdateNotificationPreferenceInput> preferences,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
