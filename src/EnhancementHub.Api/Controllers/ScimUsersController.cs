using EnhancementHub.Application.Features.Scim.Commands;
using EnhancementHub.Application.Features.Scim.Queries;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = ScimAuthenticationDefaults.Scheme)]
[Route("scim/v2/Users")]
public sealed class ScimUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScimUsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListScimUsersQuery(startIndex, count), cancellationToken);
        return Ok(ToScimListResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetScimUserByExternalIdQuery(id), cancellationToken);
        return user is null ? NotFound() : Ok(ToScimResource(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ScimUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProvisionScimUserCommand(
                request.ExternalId,
                request.UserName,
                request.DisplayName,
                request.Role,
                request.Active),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.ExternalId }, ToScimResource(result));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(
        string id,
        [FromBody] ScimUserPatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Active == false)
        {
            var deactivated = await _mediator.Send(new DeactivateScimUserCommand(id), cancellationToken);
            return deactivated ? NoContent() : NotFound();
        }

        var existing = await _mediator.Send(new GetScimUserByExternalIdQuery(id), cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var result = await _mediator.Send(
            new ProvisionScimUserCommand(
                id,
                request.UserName ?? existing.UserName,
                request.DisplayName ?? existing.DisplayName,
                request.Role ?? existing.Role,
                request.Active ?? existing.Active),
            cancellationToken);

        return Ok(ToScimResource(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deactivated = await _mediator.Send(new DeactivateScimUserCommand(id), cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    private static object ToScimListResponse(ScimListResponse<ScimUserResource> result) => new
    {
        schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
        totalResults = result.TotalResults,
        startIndex = result.StartIndex,
        itemsPerPage = result.ItemsPerPage,
        Resources = result.Resources.Select(ToScimResource).ToList()
    };

    private static object ToScimResource(ScimUserResource user) => new
    {
        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
        id = user.ExternalId,
        externalId = user.ExternalId,
        userName = user.UserName,
        displayName = user.DisplayName,
        active = user.Active,
        role = user.Role.ToString()
    };

    private static object ToScimResource(ScimUserResult user) => new
    {
        schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
        id = user.ExternalId,
        externalId = user.ExternalId,
        userName = user.Email,
        displayName = user.DisplayName,
        active = user.IsActive,
        role = user.Role.ToString()
    };

    public sealed record ScimUserCreateRequest(
        string ExternalId,
        string UserName,
        string DisplayName,
        UserRole Role = UserRole.Developer,
        bool Active = true);

    public sealed record ScimUserPatchRequest(
        string? UserName,
        string? DisplayName,
        UserRole? Role,
        bool? Active);
}
