using EnhancementHub.Application.Features.Policies.Commands;
using EnhancementHub.Application.Features.Policies.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/policies")]
public sealed class PoliciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListApprovalPolicyRulesQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertPolicyRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpsertApprovalPolicyRuleCommand(
                request.Id,
                request.Name,
                request.IsEnabled,
                request.Priority,
                request.MinimumRiskLevel,
                request.Department,
                request.ApplicationTier,
                request.RequiredRole,
                request.BlockApproval,
                request.Message),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteApprovalPolicyRuleCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record UpsertPolicyRequest(
        Guid? Id,
        string Name,
        bool IsEnabled,
        int Priority,
        RiskLevel? MinimumRiskLevel,
        string? Department,
        ApplicationTier? ApplicationTier,
        UserRole RequiredRole,
        bool BlockApproval,
        string Message);
}
