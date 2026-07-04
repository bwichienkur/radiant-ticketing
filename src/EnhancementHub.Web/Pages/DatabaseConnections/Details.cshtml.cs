using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Application.Features.SystemIntelligence.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.DatabaseConnections;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;
    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public Guid ConnectionId { get; private set; }
    public DatabaseSchemaDto? Schema { get; private set; }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        ConnectionId = id;
        Schema = await _mediator.Send(new GetDatabaseSchemaQuery(id), cancellationToken);
    }
}
