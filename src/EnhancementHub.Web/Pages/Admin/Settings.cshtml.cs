using EnhancementHub.Application.Admin;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Admin;

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

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Settings = await _mediator.Send(new GetSystemSettingsQuery(), cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateSystemSettingCommand(SettingId, Value), cancellationToken);
        return RedirectToPage();
    }
}
