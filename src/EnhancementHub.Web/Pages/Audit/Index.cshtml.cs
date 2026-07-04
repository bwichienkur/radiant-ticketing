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

    public IReadOnlyList<AuditLogDto> Logs { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Logs = await _mediator.Send(new ListAuditLogsQuery(EntityType, Action), cancellationToken);
}
