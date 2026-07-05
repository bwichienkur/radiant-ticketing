using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RoiModel : PageModel
{
    private readonly IMediator _mediator;

    public RoiModel(IMediator mediator) => _mediator = mediator;

    public RoiReportDto? Report { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Report = await _mediator.Send(new GetRoiReportQuery(), cancellationToken);
}
