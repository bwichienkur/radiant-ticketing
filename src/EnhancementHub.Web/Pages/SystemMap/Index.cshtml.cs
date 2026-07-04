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
    public bool IsLoading { get; private set; }
    public bool IsBuilding { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(Guid? applicationId, CancellationToken cancellationToken)
    {
        IsLoading = true;
        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        SelectedApplicationId = applicationId ?? Applications.FirstOrDefault()?.Id;

        if (SelectedApplicationId.HasValue)
        {
            Map = await _mediator.Send(new GetSystemMapQuery(SelectedApplicationId.Value), cancellationToken);
        }

        IsLoading = false;
    }

    public async Task<IActionResult> OnPostBuildAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        IsBuilding = true;
        await _mediator.Send(new BuildSystemGraphCommand(applicationId), cancellationToken);
        StatusMessage = "System graph rebuilt successfully.";
        return RedirectToPage(new { applicationId });
    }
}
