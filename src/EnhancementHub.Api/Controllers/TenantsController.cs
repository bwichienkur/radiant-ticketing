using EnhancementHub.Application.Features.Tenants.Commands;
using EnhancementHub.Application.Features.Tenants.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new RegisterTenantCommand(
                request.OrganizationName,
                request.Slug,
                request.AdminEmail,
                request.AdminPassword,
                request.AdminDisplayName,
                request.Region),
            cancellationToken));

    [Authorize]
    [HttpGet("current/billing")]
    public async Task<IActionResult> CurrentBilling(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetCurrentTenantBillingQuery(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListTenantsQuery(), cancellationToken));

    public sealed record RegisterTenantRequest(
        string OrganizationName,
        string Slug,
        string AdminEmail,
        string AdminPassword,
        string AdminDisplayName,
        TenantRegion Region);
}
