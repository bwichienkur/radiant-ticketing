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

[Authorize(Roles = "Admin")]
public class ApiKeysModel : PageModel
{
    private readonly IMediator _mediator;

    public ApiKeysModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<ServiceApiKeySummaryDto> ApiKeys { get; private set; } = [];
    public IReadOnlyList<TeamSummaryDto> Teams { get; private set; } = [];

    [BindProperty]
    public string NewKeyName { get; set; } = string.Empty;

    [BindProperty]
    public string? NewKeyDescription { get; set; }

    [BindProperty]
    public UserRole NewKeyRole { get; set; } = UserRole.Developer;

    [BindProperty]
    public Guid? NewKeyTeamId { get; set; }

    [BindProperty]
    public int? NewKeyExpiresInDays { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? CreatedApiKey { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ApiKeys = await _mediator.Send(new ListServiceApiKeysQuery(), cancellationToken);
        Teams = await _mediator.Send(new ListTeamsQuery(), cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateServiceApiKeyCommand(
                NewKeyName,
                NewKeyDescription,
                NewKeyRole,
                NewKeyTeamId,
                NewKeyExpiresInDays), cancellationToken);

            CreatedApiKey = result.ApiKey;
            StatusMessage = $"Created API key '{result.Name}' (prefix {result.KeyPrefix}…).";
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid revokeKeyId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeServiceApiKeyCommand(revokeKeyId), cancellationToken);
        StatusMessage = "API key revoked.";
        return RedirectToPage();
    }
}
