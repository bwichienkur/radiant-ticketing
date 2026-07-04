using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.Repositories.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RepositoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RepositoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ListRepositoriesQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterRepositoryCommand(
            request.ApplicationId,
            request.Name,
            request.Url,
            request.Provider,
            request.DefaultBranch,
            request.GitTokenSecretName), cancellationToken);
        return CreatedAtAction(nameof(Status), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/index")]
    public async Task<IActionResult> TriggerIndex(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerRepositoryIndexingCommand(id), cancellationToken);
        return Accepted();
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetRepositoryStatusQuery(id), cancellationToken));

    public sealed record RegisterRequest(
        Guid ApplicationId,
        string Name,
        string Url,
        ExternalTicketProvider Provider,
        string DefaultBranch = "main",
        string? GitTokenSecretName = null);
}
