using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor compliance page — redirects to <c>/Spa/Admin/Compliance</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Admin/Compliance. Append ?layout=classic only for legacy Razor debugging.")]
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

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Admin/Compliance");
        }

        Report = await _mediator.Send(new GetSoc2ReadinessReportQuery(), cancellationToken);
        RuntimeStatus = _runtimeStatus.GetStatus();
        return Page();
    }
}
