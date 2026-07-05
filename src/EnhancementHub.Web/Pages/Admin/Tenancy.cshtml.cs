using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Exceptions;
using EnhancementHub.Application.Features.Billing.Commands;
using EnhancementHub.Application.Features.Tenants.Commands;
using EnhancementHub.Application.Features.Tenants.Dtos;
using EnhancementHub.Application.Features.Tenants.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class TenancyModel : PageModel
{
    private readonly IMediator _mediator;

    public TenancyModel(IMediator mediator) => _mediator = mediator;

    public TenantBillingDto? Billing { get; private set; }
    public TenantIsolationStatus? Isolation { get; private set; }
    public IReadOnlyList<TenantSummaryDto> AllTenants { get; private set; } = [];
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        if (Request.Query["checkout"] == "success")
        {
            StatusMessage = "Checkout completed. Your subscription will update shortly.";
        }

        return Page();
    }

    public Task<IActionResult> OnPostCheckoutTeamAsync(CancellationToken cancellationToken) =>
        RedirectToCheckoutAsync(TenantPlan.Team, cancellationToken);

    public Task<IActionResult> OnPostCheckoutEnterpriseAsync(CancellationToken cancellationToken) =>
        RedirectToCheckoutAsync(TenantPlan.Enterprise, cancellationToken);

    public async Task<IActionResult> OnPostPortalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var session = await _mediator.Send(new CreateBillingPortalCommand(), cancellationToken);
            return Redirect(session.Url);
        }
        catch (ForbiddenException ex)
        {
            await LoadAsync(cancellationToken);
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task<IActionResult> RedirectToCheckoutAsync(
        TenantPlan plan,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _mediator.Send(new CreateBillingCheckoutCommand(plan), cancellationToken);
            return Redirect(session.Url);
        }
        catch (ForbiddenException ex)
        {
            await LoadAsync(cancellationToken);
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostProvisionSchemaAsync(CancellationToken cancellationToken)
    {
        try
        {
            Isolation = await _mediator.Send(new ProvisionTenantSchemaCommand(), cancellationToken);
            await LoadAsync(cancellationToken);
            StatusMessage = "Dedicated schema provisioned successfully.";
            return Page();
        }
        catch (ForbiddenException ex)
        {
            await LoadAsync(cancellationToken);
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            Billing = await _mediator.Send(new GetCurrentTenantBillingQuery(), cancellationToken);
            try
            {
                Isolation = await _mediator.Send(new GetCurrentTenantIsolationQuery(), cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                Isolation = null;
            }
        }
        catch (UnauthorizedAccessException)
        {
            AllTenants = await _mediator.Send(new ListTenantsQuery(), cancellationToken);
        }
    }
}
