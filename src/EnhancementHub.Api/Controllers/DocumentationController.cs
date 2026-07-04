using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documentation")]
public sealed class DocumentationController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentationController(IMediator mediator) => _mediator = mediator;

    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid applicationId,
        [FromQuery] DocumentationExportFormat format = DocumentationExportFormat.Both,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportDocumentationCommand(applicationId, format), cancellationToken);
        return File(
            System.Text.Encoding.UTF8.GetBytes(result.Content),
            result.ContentType,
            result.FileName);
    }
}
