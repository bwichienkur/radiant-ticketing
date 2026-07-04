using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RetentionModel : PageModel
{
    private readonly IMediator _mediator;

    public RetentionModel(IMediator mediator) => _mediator = mediator;

    public DataRetentionStatusDto? Status { get; private set; }
    public DataRetentionResultDto? LastResult { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Status = await _mediator.Send(new GetDataRetentionStatusQuery(), cancellationToken);

    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken)
    {
        LastResult = await _mediator.Send(new ApplyDataRetentionCommand(DryRun: true), cancellationToken);
        Status = await _mediator.Send(new GetDataRetentionStatusQuery(), cancellationToken);
        StatusMessage = $"Preview: would delete {LastResult.AiPromptRunsDeleted} AI prompt runs and {LastResult.AttachmentsDeleted} attachments.";
        return Page();
    }

    public async Task<IActionResult> OnPostApplyAsync(CancellationToken cancellationToken)
    {
        LastResult = await _mediator.Send(new ApplyDataRetentionCommand(DryRun: false), cancellationToken);
        Status = await _mediator.Send(new GetDataRetentionStatusQuery(), cancellationToken);
        StatusMessage = $"Applied: deleted {LastResult.AiPromptRunsDeleted} AI prompt runs and {LastResult.AttachmentsDeleted} attachments.";
        return Page();
    }
}
