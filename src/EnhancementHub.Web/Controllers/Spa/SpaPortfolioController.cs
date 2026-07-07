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
[Route("web-api/spa/portfolio")]
public sealed class SpaPortfolioController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpaPortfolioController(IMediator mediator) => _mediator = mediator;

    [HttpGet("health")]
    public async Task<IActionResult> GetPortfolioHealth(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPortfolioHealthQuery(), cancellationToken));

    [HttpGet("health/export")]
    [Authorize(Roles = "Admin,Approver")]
    public async Task<IActionResult> ExportPortfolioHealthCsv(CancellationToken cancellationToken)
    {
        var report = await _mediator.Send(new GetPortfolioHealthQuery(), cancellationToken);
        var csv = BuildPortfolioCsv(report);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "enhancementhub-portfolio-health.csv");
    }

    private static string BuildPortfolioCsv(PortfolioHealthReportDto report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("application_id,application_name,unresolved_drift,pending_requests,high_risk_pending,stale_repositories,risk_score");
        foreach (var row in report.Applications)
        {
            builder.AppendLine(string.Join(',',
                row.ApplicationId,
                EscapeCsv(row.ApplicationName),
                row.UnresolvedDriftCount,
                row.PendingRequestCount,
                row.HighRiskPendingCount,
                row.StaleRepositoryCount,
                row.RiskScore));
        }

        builder.AppendLine($"generated_at_utc,{report.GeneratedAtUtc:O}");
        return builder.ToString();
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
