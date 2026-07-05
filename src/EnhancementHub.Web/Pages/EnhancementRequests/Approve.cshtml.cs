using EnhancementHub.Application.Features.Approvals.Commands;
using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.EnhancementRequests;

[Authorize]
public class ApproveModel : PageModel
{
    private readonly IMediator _mediator;

    public ApproveModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<EnhancementRequestDto> Pending { get; private set; } = [];

    [BindProperty]
    public Guid? SelectedRequestId { get; set; }

    [BindProperty]
    public ApprovalActionType ActionType { get; set; } = ApprovalActionType.Approve;

    [BindProperty]
    public string? Comments { get; set; }

    [BindProperty]
    public string? NewComment { get; set; }

    public EnhancementRequestDetailDto? Selected { get; private set; }

    public async Task OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        Pending = await _mediator.Send(
            new ListEnhancementRequestsQuery(
                EnhancementRequestStatus.PendingApproval,
                Sort: EnhancementRequestSort.HighestRisk),
            cancellationToken);
        if (id.HasValue)
        {
            SelectedRequestId = id;
            Selected = await _mediator.Send(new GetEnhancementRequestByIdQuery(id.Value), cancellationToken);
        }
        else if (Pending.Count > 0)
        {
            SelectedRequestId = Pending[0].Id;
            Selected = await _mediator.Send(new GetEnhancementRequestByIdQuery(Pending[0].Id), cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostActionAsync(CancellationToken cancellationToken)
    {
        if (!SelectedRequestId.HasValue) return RedirectToPage();

        await _mediator.Send(new SubmitApprovalActionCommand(SelectedRequestId.Value, ActionType, Comments), cancellationToken);
        return RedirectToPage(new { id = SelectedRequestId });
    }

    public async Task<IActionResult> OnPostCommentAsync(CancellationToken cancellationToken)
    {
        if (!SelectedRequestId.HasValue || string.IsNullOrWhiteSpace(NewComment)) return RedirectToPage(new { id = SelectedRequestId });

        await _mediator.Send(new AddCommentCommand(SelectedRequestId.Value, NewComment, true), cancellationToken);
        return RedirectToPage(new { id = SelectedRequestId });
    }
}
