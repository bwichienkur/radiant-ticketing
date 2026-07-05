using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class CreateRequestModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? TemplateId { get; set; }

    public IActionResult OnGet() => Page();
}
