using EnhancementHub.Application.Features.CustomFields.Commands;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Application.Features.CustomFields.Queries;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnhancementHub.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/custom-fields")]
public sealed class CustomFieldsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomFieldsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new ListCustomFieldDefinitionsQuery(activeOnly), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertCustomFieldRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(
            new UpsertCustomFieldDefinitionCommand(
                request.Id,
                request.Key,
                request.Label,
                request.FieldType,
                request.IsRequired,
                request.IsActive,
                request.SortOrder,
                request.Options),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCustomFieldDefinitionCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record UpsertCustomFieldRequest(
        Guid? Id,
        string Key,
        string Label,
        CustomFieldType FieldType,
        bool IsRequired,
        bool IsActive,
        int SortOrder,
        IReadOnlyList<string>? Options);
}
