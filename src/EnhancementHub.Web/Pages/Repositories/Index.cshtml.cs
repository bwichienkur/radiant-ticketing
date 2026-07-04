using EnhancementHub.Application.Features.Repositories.Commands;
using EnhancementHub.Application.Features.Repositories.Dtos;
using EnhancementHub.Application.Features.Repositories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Repositories;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<RepositoryDto> Repositories { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Repositories = await _mediator.Send(new ListRepositoriesQuery(), cancellationToken);

    public async Task<IActionResult> OnPostTriggerAsync(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TriggerRepositoryIndexingCommand(id), cancellationToken);
        return RedirectToPage();
    }
}
