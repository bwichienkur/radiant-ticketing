using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

/// <summary>Legacy Razor settings — redirects to <c>/Spa/Settings/General</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Settings/General. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly IMediator _mediator;

    public SettingsModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<SystemSettingDto> Settings { get; private set; } = [];

    [BindProperty]
    public Guid SettingId { get; set; }

    [BindProperty]
    public string Value { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Settings/General");
        }

        Settings = await _mediator.Send(new GetSystemSettingsQuery(), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateSystemSettingCommand(SettingId, Value), cancellationToken);
        return RedirectToPage();
    }
}
