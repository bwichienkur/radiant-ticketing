using EnhancementHub.Application.Features.EnhancementRequests.Dtos;
using EnhancementHub.Application.Features.EnhancementRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.EnhancementRequests;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<EnhancementRequestDto> Requests { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Requests = await _mediator.Send(new ListEnhancementRequestsQuery(), cancellationToken);
}
