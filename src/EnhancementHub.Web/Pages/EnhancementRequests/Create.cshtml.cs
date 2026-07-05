using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.EnhancementRequests.Commands;
using EnhancementHub.Application.Features.Templates.Dtos;
using EnhancementHub.Application.Features.Templates.Queries;
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
    public IReadOnlyList<EnhancementTemplateSummaryDto> Templates { get; private set; } = [];

    public async Task OnGetAsync(Guid? templateId, CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);

        if (templateId.HasValue)
        {
            Input.TemplateId = templateId;
            var template = await _mediator.Send(new GetEnhancementTemplateQuery(templateId.Value), cancellationToken);
            Input.Title = template.Title;
            Input.BusinessDescription = template.BusinessDescription;
            Input.DesiredOutcome = template.DesiredOutcome;
            Input.Priority = template.Priority;
            Input.SupportingNotes = template.SupportingNotes;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
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
            Input.SupportingNotes,
            Input.TemplateId), cancellationToken);

        return RedirectToPage("Details", new { id = result.Id });
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var apps = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        Applications = new SelectList(apps, nameof(ApplicationDto.Id), nameof(ApplicationDto.Name));
        Templates = await _mediator.Send(new ListEnhancementTemplatesQuery(), cancellationToken);
    }

    public sealed class InputModel
    {
        public Guid? TemplateId { get; set; }
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
