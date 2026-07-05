using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class JobsModel : PageModel
{
    private readonly IMediator _mediator;

    public JobsModel(IMediator mediator) => _mediator = mediator;

    public BackgroundJobsStatusDto? Status { get; private set; }
    public IndexFreshnessReportDto? Freshness { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Status = await _mediator.Send(new GetBackgroundJobsStatusQuery(), cancellationToken);
        Freshness = await _mediator.Send(new GetIndexFreshnessReportQuery(), cancellationToken);
    }

    public async Task<IActionResult> OnPostRetryAsync(string jobId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RetryBackgroundJobCommand(jobId), cancellationToken);
        StatusMessage = success
            ? "Failed job requeued successfully."
            : "Could not requeue job. Retry is only available for Hangfire failed jobs.";
        return RedirectToPage();
    }
}
