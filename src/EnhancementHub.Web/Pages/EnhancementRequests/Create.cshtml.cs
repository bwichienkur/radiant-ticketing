using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnhancementHub.Web.Pages.EnhancementRequests;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList? Applications { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var apps = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        Applications = new SelectList(apps, nameof(ApplicationDto.Id), nameof(ApplicationDto.Name));
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(cancellationToken);
            return Page();
        }

        var result = await _mediator.Send(new CreateEnhancementRequestCommand(
            Input.Title,
            Input.BusinessDescription,
            Input.DesiredOutcome,
            Input.Priority,
            Input.TargetApplicationId,
            Input.RequestedDueDate,
            Input.Department,
            null,
            Input.SupportingNotes), cancellationToken);

        return RedirectToPage("Details", new { id = result.Id });
    }

    public sealed class InputModel
    {
        public string Title { get; set; } = string.Empty;
        public string BusinessDescription { get; set; } = string.Empty;
        public string DesiredOutcome { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public Guid? TargetApplicationId { get; set; }
        public DateTime? RequestedDueDate { get; set; }
        public string? Department { get; set; }
        public string? SupportingNotes { get; set; }
    }
}
