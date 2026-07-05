using EnhancementHub.Application.Features.Billing.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Roles = "Admin")]
public sealed class BillingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingController(IMediator mediator) => _mediator = mediator;

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] BillingCheckoutRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new CreateBillingCheckoutCommand(request.Plan), cancellationToken));

    [HttpPost("portal")]
    public async Task<IActionResult> Portal(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new CreateBillingPortalCommand(), cancellationToken));

    public sealed record BillingCheckoutRequest(TenantPlan Plan);
}
