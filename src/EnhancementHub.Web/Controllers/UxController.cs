using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Web.Controllers;

[ApiController]
[Authorize]
[Route("web-api/ux")]
public sealed class UxController : ControllerBase
{
    private readonly IMediator _mediator;

    public UxController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<object>());
        }

        var term = q.Trim();
        var requests = await _mediator.Send(
            new ListEnhancementRequestsQuery(Search: term),
            cancellationToken);

        var apps = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        var appMatches = apps
            .Where(a => a.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(a => new
            {
                type = "application",
                title = a.Name,
                subtitle = a.BusinessDomain ?? "Application",
                url = "/Applications/Index"
            });

        var requestMatches = requests.Take(8).Select(r => new
        {
            type = "request",
            title = r.Title,
            subtitle = $"{r.Status} · {r.Priority}",
            url = $"/Spa/RequestDetail/{r.Id}"
        }).ToList();

        var pages = GetStaticPages()
            .Where(p => p.title.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || p.keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(5)
            .Select(p => new { type = "page", p.title, subtitle = "Navigate", url = p.url });

        return Ok(pages.Concat(requestMatches).Concat(appMatches).Take(12));
    }

    [HttpGet("copilot")]
    public async Task<IActionResult> Copilot([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { answer = "Ask about requests, approvals, or high-risk items.", items = Array.Empty<object>() });
        }

        var lower = q.ToLowerInvariant();
        ListEnhancementRequestsQuery query;
        string answer;

        if (lower.Contains("high risk") || lower.Contains("critical"))
        {
            query = new ListEnhancementRequestsQuery(
                Status: EnhancementRequestStatus.PendingApproval,
                MinRisk: RiskLevel.High,
                Sort: EnhancementRequestSort.HighestRisk);
            answer = "High-risk items pending approval:";
        }
        else if (lower.Contains("awaiting analysis") || lower.Contains("analysis"))
        {
            query = new ListEnhancementRequestsQuery(Status: EnhancementRequestStatus.Submitted);
            answer = "Requests awaiting AI analysis:";
        }
        else if (lower.Contains("pending approval") || lower.Contains("approve"))
        {
            query = new ListEnhancementRequestsQuery(Status: EnhancementRequestStatus.PendingApproval);
            answer = "Requests pending approval:";
        }
        else
        {
            return await Search(q, cancellationToken);
        }

        var items = await _mediator.Send(query, cancellationToken);
        var results = items.Take(8).Select(r => new
        {
            type = "request",
            title = r.Title,
            subtitle = $"{r.LatestRiskLevel?.ToString() ?? "—"} risk · {r.Status}",
            url = $"/Spa/RequestDetail/{r.Id}"
        });

        return Ok(new { answer, items = results });
    }

    private static IEnumerable<(string title, string url, string[] keywords)> GetStaticPages() =>
    [
        ("Dashboard", "/Index", ["home", "overview", "metrics"]),
        ("Enhancement Requests", "/EnhancementRequests/Index", ["requests", "intake"]),
        ("Approval Queue", "/Spa/ApprovalQueue", ["approve", "pending"]),
        ("New Request", "/EnhancementRequests/Create", ["create", "submit"]),
        ("System Map", "/Spa/SystemMap", ["graph", "architecture", "map"]),
        ("Onboarding Wizard", "/Spa/OnboardingWizard", ["setup", "onboard", "wizard"]),
        ("Schema Drift", "/SchemaDrift/Index", ["drift", "schema"]),
        ("ROI Dashboard", "/Admin/Roi", ["roi", "metrics", "admin"]),
        ("Tenancy & Billing", "/Admin/Tenancy", ["tenant", "billing", "commercial"])
    ];
}
