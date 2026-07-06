using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Features.Reporting.Dtos;

public sealed record RoiReportDto(
    int TotalAnalysesCompleted,
    double AverageAnalysisDurationMinutes,
    double EstimatedManualAnalysisHoursPerRequest,
    double EstimatedHoursSaved,
    int HighOrCriticalRiskApprovedCount,
    int DriftFindingsResolved,
    int DriftFindingsTotal,
    int ArchitectEditsRecorded,
    int HumanApprovedFindings,
    int AiSuggestedFindings,
    IReadOnlyList<RoiCategoryMetricDto> TemplateUsageByCategory,
    double? AverageTimeToAnalysisHours,
    double? AverageTimeToApprovalHours,
    double MockAiRunPercent,
    int TotalAiRunsCompleted,
    double? AveragePilotNps,
    int TotalFeedbackSubmissions);

public sealed record RoiCategoryMetricDto(
    string Category,
    int RequestCount);
