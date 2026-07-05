using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class OnboardingWizardModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public Guid? SessionId => Id;

    public IActionResult OnGet() => Page();
}
