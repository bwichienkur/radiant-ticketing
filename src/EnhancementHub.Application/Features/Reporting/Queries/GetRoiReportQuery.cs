using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Features.Reporting.Queries;

public sealed record GetRoiReportQuery : IRequest<RoiReportDto>;

public sealed class GetRoiReportQueryHandler : IRequestHandler<GetRoiReportQuery, RoiReportDto>
{
    private const double ManualAnalysisHoursBaseline = 4.0;

    private readonly IReportingDbContext _dbContext;

    public GetRoiReportQueryHandler(IReportingDbContext dbContext) => _dbContext = dbContext;

    public async Task<RoiReportDto> Handle(
        GetRoiReportQuery request,
        CancellationToken cancellationToken)
    {
        var completedRuns = await _dbContext.AiPromptRuns
            .AsNoTracking()
            .Where(r => r.CompletedAt != null && r.EnhancementAnalysisId != null)
            .Select(r => new { r.StartedAt, r.CompletedAt })
            .ToListAsync(cancellationToken);

        var totalAnalyses = completedRuns.Count;
        var averageMinutes = totalAnalyses == 0
            ? 0
            : completedRuns.Average(r => (r.CompletedAt!.Value - r.StartedAt).TotalMinutes);

        var actualHours = averageMinutes / 60.0;
        var hoursSavedPerAnalysis = Math.Max(0, ManualAnalysisHoursBaseline - actualHours);
        var estimatedHoursSaved = hoursSavedPerAnalysis * totalAnalyses;

        var approvedRequestIds = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.Status == EnhancementRequestStatus.Approved)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var highRiskApproved = (await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .Where(a => approvedRequestIds.Contains(a.EnhancementRequestId))
            .GroupBy(a => a.EnhancementRequestId)
            .Select(g => g.OrderByDescending(a => a.Version).First().RiskLevel)
            .ToListAsync(cancellationToken))
            .Count(r => r == RiskLevel.High || r == RiskLevel.Critical);

        var driftTotal = await _dbContext.SchemaDriftFindings.AsNoTracking().CountAsync(cancellationToken);
        var driftResolved = await _dbContext.SchemaDriftFindings
            .AsNoTracking()
            .CountAsync(f => f.IsResolved, cancellationToken);

        var architectEdits = await _dbContext.ApprovalActions
            .AsNoTracking()
            .CountAsync(a => a.ActionType == ApprovalActionType.EditRequirements, cancellationToken);

        var aiSuggestedFindings = await _dbContext.AnalysisFindings
            .AsNoTracking()
            .CountAsync(f => f.IsAiSuggested, cancellationToken);

        var humanApprovedFindings = await _dbContext.AnalysisFindings
            .AsNoTracking()
            .CountAsync(f => f.IsHumanApproved, cancellationToken);

        var templateUsage = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Where(r => r.SupportingNotes != null && r.SupportingNotes.Contains("Template:"))
            .Select(r => r.SupportingNotes!)
            .ToListAsync(cancellationToken);

        var byCategory = templateUsage
            .Select(n =>
            {
                var start = n.IndexOf("Template:", StringComparison.Ordinal);
                if (start < 0)
                {
                    return "Unknown";
                }

                var rest = n[(start + "Template:".Length)..].Trim();
                var end = rest.IndexOf('|', StringComparison.Ordinal);
                return end >= 0 ? rest[..end].Trim() : rest;
            })
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RoiCategoryMetricDto(g.Key, g.Count()))
            .OrderByDescending(x => x.RequestCount)
            .ToList();

        var requests = await _dbContext.EnhancementRequests
            .AsNoTracking()
            .Select(r => new { r.Id, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var firstAnalyses = await _dbContext.EnhancementAnalyses
            .AsNoTracking()
            .GroupBy(a => a.EnhancementRequestId)
            .Select(g => new
            {
                EnhancementRequestId = g.Key,
                FirstAnalysisAt = g.Min(a => a.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var timeToAnalysisHours = new List<double>();
        foreach (var analysis in firstAnalyses)
        {
            var requestCreatedAt = requests
                .FirstOrDefault(r => r.Id == analysis.EnhancementRequestId)
                ?.CreatedAt;
            if (requestCreatedAt.HasValue)
            {
                timeToAnalysisHours.Add((analysis.FirstAnalysisAt - requestCreatedAt.Value).TotalHours);
            }
        }

        double? averageTimeToAnalysisHours = timeToAnalysisHours.Count > 0
            ? timeToAnalysisHours.Average()
            : null;

        var approvalActions = await _dbContext.ApprovalActions
            .AsNoTracking()
            .Where(a => a.ActionType == ApprovalActionType.Approve)
            .Select(a => new { a.EnhancementRequestId, a.CreatedAt })
            .ToListAsync(cancellationToken);

        double? averageTimeToApprovalHours = null;
        if (approvalActions.Count > 0)
        {
            var approvalDurations = new List<double>();
            foreach (var action in approvalActions)
            {
                var requestCreatedAt = requests
                    .FirstOrDefault(r => r.Id == action.EnhancementRequestId)
                    ?.CreatedAt;
                if (requestCreatedAt.HasValue)
                {
                    approvalDurations.Add((action.CreatedAt - requestCreatedAt.Value).TotalHours);
                }
            }

            if (approvalDurations.Count > 0)
            {
                averageTimeToApprovalHours = approvalDurations.Average();
            }
        }

        var completedAiRuns = await _dbContext.AiPromptRuns
            .AsNoTracking()
            .Where(r => r.CompletedAt != null)
            .Select(r => r.ModelName)
            .ToListAsync(cancellationToken);

        var totalAiRunsCompleted = completedAiRuns.Count;
        var mockAiRuns = completedAiRuns.Count(m =>
            m.Contains("mock", StringComparison.OrdinalIgnoreCase));
        var mockAiRunPercent = totalAiRunsCompleted == 0
            ? 0
            : Math.Round(mockAiRuns * 100.0 / totalAiRunsCompleted, 1);

        var feedbackScores = await _dbContext.ProductFeedbacks
            .AsNoTracking()
            .Select(f => f.NpsScore)
            .ToListAsync(cancellationToken);

        double? averagePilotNps = feedbackScores.Count > 0
            ? Math.Round(feedbackScores.Average(), 1)
            : null;

        return new RoiReportDto(
            totalAnalyses,
            Math.Round(averageMinutes, 1),
            ManualAnalysisHoursBaseline,
            Math.Round(estimatedHoursSaved, 1),
            highRiskApproved,
            driftResolved,
            driftTotal,
            architectEdits,
            humanApprovedFindings,
            aiSuggestedFindings,
            byCategory,
            averageTimeToAnalysisHours.HasValue
                ? Math.Round(averageTimeToAnalysisHours.Value, 2)
                : null,
            averageTimeToApprovalHours.HasValue
                ? Math.Round(averageTimeToApprovalHours.Value, 2)
                : null,
            mockAiRunPercent,
            totalAiRunsCompleted,
            averagePilotNps,
            feedbackScores.Count);
    }
}
