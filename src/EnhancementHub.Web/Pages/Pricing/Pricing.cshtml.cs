using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Pricing;

[AllowAnonymous]
public class PricingModel : PageModel
{
    public void OnGet()
    {
    }
}
