using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services;

public sealed class SlaEscalationService : ISlaEscalationService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SlaEscalationService> _logger;

    public SlaEscalationService(
        IEnhancementHubDbContext dbContext,
        INotificationService notificationService,
        ILogger<SlaEscalationService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> ProcessEscalationsAsync(CancellationToken cancellationToken = default)
    {
        var slaRules = await _dbContext.ApprovalPolicyRules
            .AsNoTracking()
            .Where(r => r.IsEnabled && r.EscalateOnBreach && r.SlaTargetHours.HasValue && r.SlaTargetHours > 0)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        if (slaRules.Count == 0)
        {
            return 0;
        }

        var minSlaHours = slaRules.Min(r => r.SlaTargetHours!.Value);
        var cutoff = DateTime.UtcNow.AddHours(-minSlaHours);

        var pendingRequests = await _dbContext.EnhancementRequests
            .Where(r => r.Status == EnhancementRequestStatus.PendingApproval && r.CreatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        var escalated = 0;

        foreach (var request in pendingRequests)
        {
            var hoursOpen = (DateTime.UtcNow - request.CreatedAt).TotalHours;
            var matchingRule = slaRules
                .Where(r => hoursOpen >= r.SlaTargetHours!.Value)
                .OrderByDescending(r => r.SlaTargetHours)
                .FirstOrDefault();

            if (matchingRule is null)
            {
                continue;
            }

            if (request.LastSlaEscalationAt.HasValue
                && request.LastSlaEscalationAt.Value > DateTime.UtcNow.AddHours(-6))
            {
                continue;
            }

            var role = matchingRule.EscalateToRole ?? UserRole.Admin;
            var recipients = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Role == role)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "SLA breach for request {RequestId} but no active users with role {Role}",
                    request.Id,
                    role);
                continue;
            }

            foreach (var userId in recipients)
            {
                await _notificationService.NotifyUserAsync(
                    userId,
                    NotificationType.SlaEscalation,
                    "SLA escalation",
                    $"Request '{request.Title}' has been pending approval for {hoursOpen:0.#} hours (SLA: {matchingRule.SlaTargetHours}h).",
                    nameof(EnhancementRequest),
                    request.Id,
                    $"/Spa/ApprovalQueue/{request.Id}",
                    cancellationToken: cancellationToken);
            }

            request.LastSlaEscalationAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            escalated++;
        }

        if (escalated > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SLA escalations sent for {Count} request(s).", escalated);
        }

        return escalated;
    }
}
