using EnhancementHub.Application.Features.Admin.Commands;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Application.Features.Admin.Queries;
using EnhancementHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor team detail — redirects to <c>/Spa/Settings/Teams</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Settings/Teams. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class TeamDetailModel : PageModel
{
    private readonly IMediator _mediator;

    public TeamDetailModel(IMediator mediator) => _mediator = mediator;

    public TeamDetailDto? Team { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid TeamId { get; set; }

    [BindProperty]
    public string InviteEmail { get; set; } = string.Empty;

    [BindProperty]
    public string InviteDisplayName { get; set; } = string.Empty;

    [BindProperty]
    public UserRole InviteGlobalRole { get; set; } = UserRole.Developer;

    [BindProperty]
    public string InviteTeamRole { get; set; } = TeamMemberRoles.Member;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Settings/Teams");
        }

        TeamId = Id;
        Team = await _mediator.Send(new GetTeamDetailQuery(Id), cancellationToken);
        return Team is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostInviteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new AddTeamMemberCommand(
                TeamId,
                InviteEmail,
                InviteDisplayName,
                InviteGlobalRole,
                InviteTeamRole), cancellationToken);

            StatusMessage = result.UserCreated
                ? $"Invited {result.Email}. Temporary password: {result.TemporaryPassword}"
                : $"Added existing user {result.Email} to the team.";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = TeamId });
    }

    public async Task<IActionResult> OnPostUpdateRoleAsync(
        Guid memberId,
        string newTeamRole,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new UpdateTeamMemberRoleCommand(TeamId, memberId, newTeamRole), cancellationToken);
            StatusMessage = "Team role updated.";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = TeamId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveTeamMemberCommand(TeamId, memberId), cancellationToken);
        StatusMessage = "Member removed from team.";
        return RedirectToPage(new { id = TeamId });
    }
}
