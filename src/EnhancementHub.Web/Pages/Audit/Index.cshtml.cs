using EnhancementHub.Application.AuditLogs;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Audit;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public IReadOnlyList<AuditLogDto> Logs { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Logs = await _mediator.Send(
            new ListAuditLogsQuery(EntityType, Action, null, From, To),
            cancellationToken);

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> OnGetExportAsync(string format, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExportAuditLogsQuery(format, EntityType, Action, null, From, To),
            cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }
}
