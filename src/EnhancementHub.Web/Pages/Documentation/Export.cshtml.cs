using System.Text;
using EnhancementHub.Application.Features.Applications.Dtos;
using EnhancementHub.Application.Features.Applications.Queries;
using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Documentation;

/// <summary>Legacy Razor documentation export — redirects to <c>/Spa/Documentation/Export</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Documentation/Export. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class ExportModel : PageModel
{
    private readonly IMediator _mediator;
    public ExportModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public Guid ApplicationId { get; set; }

    [BindProperty]
    public DocumentationExportFormat Format { get; set; } = DocumentationExportFormat.Both;

    public IReadOnlyList<ApplicationDto> Applications { get; private set; } = [];
    public string? Preview { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPagePermanent("/Spa/DocumentationExport");
        }

        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        ApplicationId = Applications.FirstOrDefault()?.Id ?? Guid.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Applications = await _mediator.Send(new ListApplicationsQuery(), cancellationToken);
        var result = await _mediator.Send(new ExportDocumentationCommand(ApplicationId, Format), cancellationToken);
        return File(Encoding.UTF8.GetBytes(result.Content), result.ContentType, result.FileName);
    }
}
