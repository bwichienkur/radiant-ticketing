using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    public void OnGet() { }
}
