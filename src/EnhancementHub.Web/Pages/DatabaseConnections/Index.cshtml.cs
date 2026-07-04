using EnhancementHub.Application.Features.SystemIntelligence.Commands;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;
    public IReadOnlyList<DatabaseConnectionDto> Connections { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Connections = await _mediator.Send(new ListDatabaseConnectionsQuery(), cancellationToken);

    public async Task<IActionResult> OnPostScanAsync(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerDatabaseScanCommand(id), cancellationToken);
        return RedirectToPage();
    }
}
