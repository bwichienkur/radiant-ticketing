using EnhancementHub.Application.Abstractions;
using EnhancementHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Account;

/// <summary>Legacy Razor notification preferences — redirects to <c>/Spa/Account/Notifications</c> unless <c>?layout=classic</c>.</summary>
[Obsolete("Use /Spa/Account/Notifications. Append ?layout=classic only for legacy Razor debugging.")]
[Authorize]
public class NotificationPreferencesModel : PageModel
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationPreferencesModel(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<NotificationPreferenceDto> Preferences { get; private set; } = [];

    [BindProperty]
    public List<PreferenceInput> PreferenceInputs { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string? layout, CancellationToken cancellationToken)
    {
        if (!string.Equals(layout, "classic", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent("/Spa/Account/Notifications");
        }

        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        Preferences = await _notificationService.GetPreferencesAsync(userId, cancellationToken);
        PreferenceInputs = Preferences
            .Select(p => new PreferenceInput
            {
                Type = p.Type,
                EmailEnabled = p.EmailEnabled,
                InAppEnabled = p.InAppEnabled
            })
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        var updates = PreferenceInputs
            .Select(p => new UpdateNotificationPreferenceInput(p.Type, p.EmailEnabled, p.InAppEnabled))
            .ToList();

        await _notificationService.UpdatePreferencesAsync(userId, updates, cancellationToken);
        TempData["StatusMessage"] = "Notification preferences saved.";
        return RedirectToPage();
    }

    public sealed class PreferenceInput
    {
        public NotificationType Type { get; set; }
        public bool EmailEnabled { get; set; }
        public bool InAppEnabled { get; set; }
    }
}
