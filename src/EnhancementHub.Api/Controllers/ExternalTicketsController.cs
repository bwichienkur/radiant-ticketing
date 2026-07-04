using EnhancementHub.Application.Features.ExternalTickets.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ExternalTicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExternalTicketsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{requestId:guid}/export")]
    public async Task<IActionResult> Export(Guid requestId, [FromBody] ExportRequest request, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new ExportExternalTicketCommand(requestId, request.Provider), cancellationToken));

    public sealed record ExportRequest(ExternalTicketProvider Provider);
}
