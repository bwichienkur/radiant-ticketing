using System.Globalization;
using System.Text;
using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers.Spa;

[ApiController]
[Authorize(Roles = "Admin,Approver")]
[Route("web-api/spa/insights")]
public sealed class SpaInsightsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaInsightsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("roi")]
    public async Task<IActionResult> GetRoiReport(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetRoiReportQuery(), cancellationToken));

    [HttpGet("roi/export")]
    public async Task<IActionResult> ExportRoiCsv(CancellationToken cancellationToken)
    {
        var report = await _mediator.Send(new GetRoiReportQuery(), cancellationToken);
        var csv = BuildRoiCsv(report);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "enhancementhub-roi-report.csv");
    }

    private static string BuildRoiCsv(RoiReportDto report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine($"analyses_completed,{report.TotalAnalysesCompleted}");
        builder.AppendLine($"avg_analysis_duration_minutes,{report.AverageAnalysisDurationMinutes.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"manual_baseline_hours,{report.EstimatedManualAnalysisHoursPerRequest.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"estimated_hours_saved,{report.EstimatedHoursSaved.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"high_critical_approved,{report.HighOrCriticalRiskApprovedCount}");
        builder.AppendLine($"drift_resolved,{report.DriftFindingsResolved}");
        builder.AppendLine($"drift_total,{report.DriftFindingsTotal}");
        builder.AppendLine($"architect_edits,{report.ArchitectEditsRecorded}");
        builder.AppendLine($"human_approved_findings,{report.HumanApprovedFindings}");
        builder.AppendLine($"ai_suggested_findings,{report.AiSuggestedFindings}");
        builder.AppendLine($"avg_time_to_analysis_hours,{FormatNullable(report.AverageTimeToAnalysisHours)}");
        builder.AppendLine($"avg_time_to_approval_hours,{FormatNullable(report.AverageTimeToApprovalHours)}");
        builder.AppendLine($"mock_ai_run_percent,{report.MockAiRunPercent.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"total_ai_runs,{report.TotalAiRunsCompleted}");
        builder.AppendLine($"pilot_nps,{FormatNullable(report.AveragePilotNps)}");
        builder.AppendLine($"feedback_submissions,{report.TotalFeedbackSubmissions}");

        foreach (var category in report.TemplateUsageByCategory)
        {
            builder.AppendLine(
                $"template_{EscapeCsv(category.Category)},{category.RequestCount}");
        }

        return builder.ToString();
    }

    private static string FormatNullable(double? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static string EscapeCsv(string value) =>
        value.Replace(" ", "_", StringComparison.Ordinal);
}
