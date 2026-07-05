using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class RequestDetailModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public Guid RequestId => Id;

    public IActionResult OnGet()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/EnhancementRequests/Index");
        }

        return Page();
    }
}
