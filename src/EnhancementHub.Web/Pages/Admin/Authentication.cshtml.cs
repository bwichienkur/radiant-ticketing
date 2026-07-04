using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Application.Features.Admin.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuthenticationModel : PageModel
{
    private readonly IMediator _mediator;

    public AuthenticationModel(IMediator mediator) => _mediator = mediator;

    public AuthenticationConfigurationStatusDto? Status { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Status = await _mediator.Send(new GetAuthenticationConfigurationStatusQuery(), cancellationToken);
}
