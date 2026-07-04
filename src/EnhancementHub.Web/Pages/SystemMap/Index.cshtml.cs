using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.SystemMap;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<ApplicationDto> Applications { get; private set; } = [];
    public SystemMapDto? Map { get; private set; }
    public Guid? SelectedApplicationId { get; private set; }

    public async Task OnGetAsync(Guid? applicationId, CancellationToken cancellationToken)
    {
        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        SelectedApplicationId = applicationId ?? Applications.FirstOrDefault()?.Id;

        if (SelectedApplicationId.HasValue)
        {
            Map = await _mediator.Send(new GetSystemMapQuery(SelectedApplicationId.Value), cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostBuildAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new BuildSystemGraphCommand(applicationId), cancellationToken);
        return RedirectToPage(new { applicationId });
    }
}
