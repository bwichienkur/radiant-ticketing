using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/database-connections")]
public sealed class DatabaseConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DatabaseConnectionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid? applicationId, CancellationToken cancellationToken) =>
        Execute(() => _mediator.Send(new ListDatabaseConnectionsQuery(applicationId), cancellationToken));

    [HttpPost]
    public Task<IActionResult> Register([FromBody] RegisterDatabaseConnectionCommand command, CancellationToken cancellationToken) =>
        Execute(() => _mediator.Send(command, cancellationToken), 201);

    [HttpPost("{id:guid}/scan")]
    public Task<IActionResult> Scan(Guid id, CancellationToken cancellationToken) =>
        Execute(() => _mediator.Send(new TriggerDatabaseScanCommand(id), cancellationToken));

    [HttpGet("{id:guid}/schema")]
    public Task<IActionResult> Schema(Guid id, CancellationToken cancellationToken) =>
        Execute(() => _mediator.Send(new GetDatabaseSchemaQuery(id), cancellationToken));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action, int success = 200)
    {
        var result = await action();
        return StatusCode(success, result);
    }
}
