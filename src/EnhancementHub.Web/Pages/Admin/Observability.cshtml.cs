using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ObservabilityModel : PageModel
{
    private readonly IMediator _mediator;

    public ObservabilityModel(IMediator mediator) => _mediator = mediator;

    public ObservabilityStatusDto? Status { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Status = await _mediator.Send(new GetObservabilityStatusQuery(), cancellationToken);
}
