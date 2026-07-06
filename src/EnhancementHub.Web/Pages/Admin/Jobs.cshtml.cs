using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor jobs page — redirects to <c>/Spa/Admin/Jobs</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Admin/Jobs. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class JobsModel : PageModel
{
    private readonly IMediator _mediator;

    public JobsModel(IMediator mediator) => _mediator = mediator;

    public BackgroundJobsStatusDto? Status { get; private set; }
    public IndexFreshnessReportDto? Freshness { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Admin/Jobs");
        }

        Status = await _mediator.Send(new GetBackgroundJobsStatusQuery(), cancellationToken);
        Freshness = await _mediator.Send(new GetIndexFreshnessReportQuery(), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRetryAsync(string jobId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RetryBackgroundJobCommand(jobId), cancellationToken);
        StatusMessage = success
            ? "Failed job requeued successfully."
            : "Could not requeue job. Retry is only available for Hangfire failed jobs.";
        return RedirectToPage("/Spa/Admin/Jobs");
    }
}
