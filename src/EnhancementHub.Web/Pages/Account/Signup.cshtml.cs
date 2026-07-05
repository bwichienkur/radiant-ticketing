using EnhancementHub.Application.Features.Tenants.Commands;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnhancementHub.Web.Pages.Account;

[AllowAnonymous]
public class SignupModel : PageModel
{
    private readonly IMediator _mediator;

    public SignupModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList Regions { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        Regions = new SelectList(Enum.GetValues<TenantRegion>(), Input.Region);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Regions = new SelectList(Enum.GetValues<TenantRegion>(), Input.Region);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _mediator.Send(
                new RegisterTenantCommand(
                    Input.OrganizationName,
                    Input.Slug,
                    Input.AdminEmail,
                    Input.AdminPassword,
                    Input.AdminDisplayName,
                    Input.Region),
                cancellationToken);

            var login = new Application.Common.Models.LoginResult(
                result.Token,
                result.AdminUserId,
                result.AdminEmail,
                Input.AdminDisplayName,
                UserRole.Admin,
                result.TenantId);

            await HttpContext.SignInAsync(
                "Cookies",
                DevAuthService.CreatePrincipal(login));

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public sealed class InputModel
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public TenantRegion Region { get; set; } = TenantRegion.US;
        public string AdminDisplayName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }
}
