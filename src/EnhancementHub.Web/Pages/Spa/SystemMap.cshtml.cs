using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class SystemMapModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? ApplicationId { get; set; }

    public IActionResult OnGet() => Page();
}
