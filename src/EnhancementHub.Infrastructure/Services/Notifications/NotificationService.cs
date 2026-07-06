using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class NotificationService : INotificationService
{
    private static readonly IReadOnlyDictionary<NotificationType, string> TypeLabels = new Dictionary<NotificationType, string>
    {
        [NotificationType.ApprovalAssigned] = "Approval assigned",
        [NotificationType.AnalysisComplete] = "Analysis complete",
        [NotificationType.DriftCritical] = "Critical schema drift"
    };

    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IUserNotificationNotifier _notifier;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEnhancementHubDbContext dbContext,
        IUserNotificationNotifier notifier,
        IEmailSender emailSender,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _notifier = notifier;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<NotificationDto> NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? entityType = null,
        Guid? entityId = null,
        string? actionUrl = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var preferences = await GetOrCreatePreferenceAsync(userId, type, cancellationToken);
        NotificationDto? dto = null;

        if (preferences.InAppEnabled)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                EntityType = entityType,
                EntityId = entityId,
                ActionUrl = actionUrl,
                TenantId = tenantId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);
            dto = ToDto(notification);

            await _notifier.NotifyUserAsync(
                userId,
                new UserNotificationPayload(
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Message,
                    notification.EntityType,
                    notification.EntityId,
                    notification.ActionUrl,
                    notification.IsRead,
                    notification.CreatedAt),
                cancellationToken);
        }

        if (preferences.EmailEnabled)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    await _emailSender.SendAsync(
                        user.Email,
                        $"[{TypeLabels[type]}] {title}",
                        $"{message}\n\n{(actionUrl is not null ? $"Open: {actionUrl}\n" : string.Empty)}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification email to user {UserId}", userId);
                }
            }
        }

        return dto ?? new NotificationDto(
            Guid.Empty,
            type,
            title,
            message,
            true,
            null,
            entityType,
            entityId,
            actionUrl,
            DateTime.UtcNow);
    }

    public async Task NotifyApproversOfPendingApprovalAsync(
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var approvers = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.Approver))
            .ToListAsync(cancellationToken);

        var actionUrl = $"/Spa/RequestDetail/{enhancementRequestId}";
        foreach (var approver in approvers)
        {
            await NotifyUserAsync(
                approver.Id,
                NotificationType.ApprovalAssigned,
                "Request pending approval",
                $"\"{requestTitle}\" is ready for your review.",
                nameof(EnhancementRequest),
                enhancementRequestId,
                actionUrl,
                tenantId,
                cancellationToken);
        }
    }

    public async Task NotifySubmitterOfAnalysisCompleteAsync(
        Guid submitterUserId,
        Guid enhancementRequestId,
        string requestTitle,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await NotifyUserAsync(
            submitterUserId,
            NotificationType.AnalysisComplete,
            "AI analysis complete",
            $"Analysis for \"{requestTitle}\" is complete and awaiting approval.",
            nameof(EnhancementRequest),
            enhancementRequestId,
            $"/Spa/RequestDetail/{enhancementRequestId}",
            tenantId,
            cancellationToken);
    }

    public async Task NotifyAdminsOfCriticalDriftAsync(
        Guid databaseConnectionId,
        string connectionName,
        int criticalCount,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var recipients = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.Approver))
            .ToListAsync(cancellationToken);

        var actionUrl = $"/Spa/SchemaDrift?connectionId={databaseConnectionId}";
        foreach (var recipient in recipients)
        {
            await NotifyUserAsync(
                recipient.Id,
                NotificationType.DriftCritical,
                "Critical schema drift detected",
                $"{criticalCount} critical drift finding(s) detected for {connectionName}.",
                nameof(DatabaseConnection),
                databaseConnectionId,
                actionUrl,
                tenantId,
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> ListForUserAsync(
        Guid userId,
        bool unreadOnly,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return items.Select(ToDto).ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        return Enum.GetValues<NotificationType>()
            .Select(type =>
            {
                var preference = existing.FirstOrDefault(p => p.Type == type);
                return new NotificationPreferenceDto(
                    type,
                    TypeLabels[type],
                    preference?.EmailEnabled ?? true,
                    preference?.InAppEnabled ?? true);
            })
            .ToList();
    }

    public async Task UpdatePreferencesAsync(
        Guid userId,
        IReadOnlyList<UpdateNotificationPreferenceInput> preferences,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var input in preferences)
        {
            var preference = existing.FirstOrDefault(p => p.Type == input.Type);
            if (preference is null)
            {
                preference = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = input.Type,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.NotificationPreferences.Add(preference);
            }

            preference.EmailEnabled = input.EmailEnabled;
            preference.InAppEnabled = input.InAppEnabled;
            preference.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<NotificationPreference> GetOrCreatePreferenceAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            EmailEnabled = true,
            InAppEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.NotificationPreferences.Add(preference);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return preference;
    }

    private static NotificationDto ToDto(Notification notification) =>
        new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.ReadAt,
            notification.EntityType,
            notification.EntityId,
            notification.ActionUrl,
            notification.CreatedAt);
}
