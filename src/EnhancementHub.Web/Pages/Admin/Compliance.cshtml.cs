using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ComplianceModel : PageModel
{
    private readonly IMediator _mediator;

    public ComplianceModel(IMediator mediator) => _mediator = mediator;

    public Soc2ReadinessReportDto? Report { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Report = await _mediator.Send(new GetSoc2ReadinessReportQuery(), cancellationToken);
}
