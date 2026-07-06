using EnhancementHub.Application.Features.Search.Queries;
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
        var result = await _mediator.Send(new GlobalEntitySearchQuery(q), cancellationToken);
        return Ok(result.Items);
    }

    [HttpGet("copilot")]
    public async Task<IActionResult> Copilot([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { answer = "Try keywords like high risk, pending approval, or system map.", items = Array.Empty<object>() });
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

        var items = (await _mediator.Send(query, cancellationToken)).Items;
        var results = items.Take(8).Select(r => new
        {
            type = "request",
            title = r.Title,
            subtitle = $"{r.LatestRiskLevel?.ToString() ?? "—"} risk · {r.Status}",
            url = $"/Spa/RequestDetail/{r.Id}"
        });

        return Ok(new { answer, items = results });
    }
}
