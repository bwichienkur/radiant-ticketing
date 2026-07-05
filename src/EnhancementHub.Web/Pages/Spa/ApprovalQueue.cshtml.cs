using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Spa;

[Authorize]
public class ApprovalQueueModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public Guid? RequestId => Id;

    public IActionResult OnGet() => Page();
}
