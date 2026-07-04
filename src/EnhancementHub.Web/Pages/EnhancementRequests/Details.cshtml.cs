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
    public IReadOnlyList<ApprovalActionDto> ApprovalHistory { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
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

        return Page();
    }
}
