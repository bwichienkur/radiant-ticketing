using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class TeamsModel : PageModel
{
    private readonly IMediator _mediator;

    public TeamsModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<TeamSummaryDto> Teams { get; private set; } = [];

    [BindProperty]
    public string NewTeamName { get; set; } = string.Empty;

    [BindProperty]
    public string? NewTeamDescription { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Teams = await _mediator.Send(new ListTeamsQuery(), cancellationToken);

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(
                new CreateTeamCommand(NewTeamName, NewTeamDescription),
                cancellationToken);
            StatusMessage = $"Created team '{team.Name}'.";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
