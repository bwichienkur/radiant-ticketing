using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class ApplicationsModel : PageModel
{
    public void OnGet() { }
}
