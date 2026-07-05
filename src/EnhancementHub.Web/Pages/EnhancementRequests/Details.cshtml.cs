using EnhancementHub.Application.Features.Analysis.Dtos;
using EnhancementHub.Application.Features.Analysis.Queries;
using EnhancementHub.Application.Features.Approvals.Dtos;
using EnhancementHub.Application.Features.Approvals.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.EnhancementRequests;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public EnhancementRequestDetailDto? Detail { get; private set; }
    public EnhancementAnalysisDto? Analysis { get; private set; }
    public AnalysisComparisonDto? Comparison { get; private set; }
    public IReadOnlyList<ApprovalActionDto> ApprovalHistory { get; private set; } = [];

    [BindProperty]
    public string? NewComment { get; set; }

    [BindProperty]
    public bool CommentIsInternal { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(Guid id, string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/Spa/RequestDetail", new { id });
        }

        Detail = await _mediator.Send(new GetEnhancementRequestByIdQuery(id), cancellationToken);
        ApprovalHistory = await _mediator.Send(new GetApprovalHistoryQuery(id), cancellationToken);

        if (Detail.Analyses.Count > 0)
        {
            try
            {
                Analysis = await _mediator.Send(new GetEnhancementAnalysisQuery(id), cancellationToken);
            }
            catch
            {
                Analysis = null;
            }
        }

        if (Detail.Analyses.Count >= 2)
        {
            var versions = Detail.Analyses.OrderBy(a => a.Version).Select(a => a.Version).ToList();
            var versionA = versions[^2];
            var versionB = versions[^1];
            try
            {
                Comparison = await _mediator.Send(
                    new GetAnalysisComparisonQuery(id, versionA, versionB),
                    cancellationToken);
            }
            catch
            {
                Comparison = null;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCommentAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewComment))
        {
            return RedirectToPage(new { id });
        }

        await _mediator.Send(new Application.Features.Approvals.Commands.AddCommentCommand(id, NewComment, CommentIsInternal), cancellationToken);
        return RedirectToPage("/Spa/RequestDetail", new { id });
    }
}
