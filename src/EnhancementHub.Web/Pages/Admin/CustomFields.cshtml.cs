using EnhancementHub.Application.Features.CustomFields.Commands;
using EnhancementHub.Application.Features.CustomFields.Dtos;
using EnhancementHub.Application.Features.CustomFields.Queries;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CustomFieldsModel : PageModel
{
    private readonly IMediator _mediator;

    public CustomFieldsModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<CustomFieldDefinitionDto> Fields { get; private set; } = [];

    [BindProperty]
    public Guid? EditId { get; set; }

    [BindProperty]
    public string Key { get; set; } = string.Empty;

    [BindProperty]
    public string Label { get; set; } = string.Empty;

    [BindProperty]
    public CustomFieldType FieldType { get; set; } = CustomFieldType.Text;

    [BindProperty]
    public bool IsRequired { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    [BindProperty]
    public int SortOrder { get; set; }

    [BindProperty]
    public string? OptionsCsv { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Fields = await _mediator.Send(new ListCustomFieldDefinitionsQuery(ActiveOnly: false), cancellationToken);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = string.IsNullOrWhiteSpace(OptionsCsv)
                ? null
                : OptionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            await _mediator.Send(
                new UpsertCustomFieldDefinitionCommand(
                    EditId,
                    Key,
                    Label,
                    FieldType,
                    IsRequired,
                    IsActive,
                    SortOrder,
                    options),
                cancellationToken);

            StatusMessage = EditId.HasValue ? "Custom field updated." : "Custom field created.";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid deleteId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCustomFieldDefinitionCommand(deleteId), cancellationToken);
        StatusMessage = "Custom field deleted.";
        return RedirectToPage();
    }
}
