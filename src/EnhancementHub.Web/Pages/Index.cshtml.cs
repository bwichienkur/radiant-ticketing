using EnhancementHub.Application.Features.Reporting.Dtos;
using EnhancementHub.Application.Features.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public DashboardReportDto? Report { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Report = await _mediator.Send(new GetDashboardReportQuery(), cancellationToken);
}
