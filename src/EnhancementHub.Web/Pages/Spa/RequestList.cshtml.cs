using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class RequestListModel : PageModel
{
    public bool IsApprover => User.IsInRole("Admin") || User.IsInRole("Approver");

    public void OnGet() { }
}
