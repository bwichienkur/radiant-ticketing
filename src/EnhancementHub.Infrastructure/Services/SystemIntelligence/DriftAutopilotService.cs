using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.SystemIntelligence;

public sealed class DriftAutopilotService : IDriftAutopilotService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IFeatureService _featureService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DriftAutopilotService> _logger;

    public DriftAutopilotService(
        IEnhancementHubDbContext dbContext,
        IFeatureService featureService,
        IAuditService auditService,
        ILogger<DriftAutopilotService> logger)
    {
        _dbContext = dbContext;
        _featureService = featureService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<int> AutoDraftRequestsFromDriftAsync(CancellationToken cancellationToken = default)
    {
        if (!_featureService.IsEnabled(FeatureFlags.DriftAutopilot))
        {
            return 0;
        }

        var submitter = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Admin)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (submitter is null)
        {
            _logger.LogWarning("Drift autopilot skipped: no active admin user to submit auto-drafts.");
            return 0;
        }

        var linkedFindingIds = await LoadLinkedFindingIdsAsync(cancellationToken);

        var findings = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .Include(f => f.DatabaseConnection)
            .Where(f => !f.IsResolved)
            .Where(f => f.Severity == DriftSeverity.Critical || f.Severity == DriftSeverity.High)
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.DetectedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        var created = 0;
        var now = DateTime.UtcNow;

        foreach (var finding in findings.Where(f => !linkedFindingIds.Contains(f.Id)))
        {
            var connection = finding.DatabaseConnection;
            var businessDescription = string.IsNullOrWhiteSpace(finding.Description)
                ? $"Schema drift detected: {finding.Title}"
                : finding.Description;

            if (!string.IsNullOrWhiteSpace(finding.CodeReference))
            {
                businessDescription = $"{businessDescription}\n\nCode reference: {finding.CodeReference}";
            }

            if (!string.IsNullOrWhiteSpace(finding.DatabaseReference))
            {
                businessDescription = $"{businessDescription}\n\nDatabase reference: {finding.DatabaseReference}";
            }

            var request = new EnhancementRequest
            {
                Id = Guid.NewGuid(),
                Title = $"[Autopilot] Remediate schema drift: {finding.Title}",
                BusinessDescription = businessDescription,
                DesiredOutcome = $"Align live database schema with indexed code expectations for {connection.Name}.",
                Priority = finding.Severity == DriftSeverity.Critical ? "Critical" : "High",
                TargetApplicationId = connection.ApplicationId,
                SubmittedByUserId = submitter.Id,
                SupportingNotes = DriftRequestProvenance.BuildSupportingNotes(
                    finding.Id,
                    connection.Name,
                    finding.Severity),
                Status = EnhancementRequestStatus.Submitted,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = submitter.Id,
                UpdatedBy = submitter.Id
            };

            _dbContext.EnhancementRequests.Add(request);
            linkedFindingIds.Add(finding.Id);
            created++;

            await _auditService.LogAsync(
                "DriftAutopilotDraft",
                nameof(EnhancementRequest),
                request.Id,
                $"Auto-drafted request from drift finding '{finding.Title}'.",
                cancellationToken);
        }

        if (created > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Drift autopilot created {Count} enhancement request(s).", created);
        }

        return created;
    }

    private async Task<HashSet<Guid>> LoadLinkedFindingIdsAsync(CancellationToken cancellationToken)
    {
        var notes = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.SupportingNotes != null && r.SupportingNotes.Contains(DriftRequestProvenance.MarkerPrefix))
            .Select(r => r.SupportingNotes!)
            .ToListAsync(cancellationToken);

        return notes
            .Select(DriftRequestProvenance.TryParseFindingId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
    }
}
