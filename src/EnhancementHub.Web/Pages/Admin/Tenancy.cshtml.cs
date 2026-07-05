using EnhancementHub.Application.Features.Tenants.Dtos;
using EnhancementHub.Application.Features.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class TenancyModel : PageModel
{
    private readonly IMediator _mediator;

    public TenancyModel(IMediator mediator) => _mediator = mediator;

    public TenantBillingDto? Billing { get; private set; }
    public IReadOnlyList<TenantSummaryDto> AllTenants { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Billing = await _mediator.Send(new GetCurrentTenantBillingQuery(), cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            AllTenants = await _mediator.Send(new ListTenantsQuery(), cancellationToken);
        }
    }
}
