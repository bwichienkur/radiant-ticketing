using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
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
    private readonly IPlatformRuntimeStatusService _runtimeStatus;

    public ComplianceModel(IMediator mediator, IPlatformRuntimeStatusService runtimeStatus)
    {
        _mediator = mediator;
        _runtimeStatus = runtimeStatus;
    }

    public Soc2ReadinessReportDto? Report { get; private set; }
    public PlatformRuntimeStatus RuntimeStatus { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Report = await _mediator.Send(new GetSoc2ReadinessReportQuery(), cancellationToken);
        RuntimeStatus = _runtimeStatus.GetStatus();
    }
}
