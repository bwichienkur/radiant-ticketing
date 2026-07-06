using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize(Roles = "Admin,Approver")]
public class InsightsModel : PageModel
{
    public void OnGet() { }
}
