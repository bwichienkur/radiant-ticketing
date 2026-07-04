using System.ComponentModel.DataAnnotations;
using EnhancementHub.Infrastructure.Security;
using EnhancementHub.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnhancementHub.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly DevAuthService _auth;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(
        DevAuthService auth,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _auth = auth;
        _configuration = configuration;
        _environment = environment;
    }

    [BindProperty]
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }

    public bool SsoEnabled { get; private set; }

    public bool ShowDevCredentials { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ConfigureViewFlags();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        ConfigureViewFlags();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _auth.LoginAsync(Email, Password, cancellationToken);
        if (result is null)
        {
            ErrorMessage = "Invalid email or password. Check your credentials and try again.";
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            DevAuthService.CreatePrincipal(result),
            new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });

        return RedirectToLocal(returnUrl);
    }

    public IActionResult OnGetSso(string? returnUrl = null)
    {
        if (!OpenIdConnectAuthenticationExtensions.IsOpenIdConnectEnabled(_configuration))
        {
            ErrorMessage = "Single sign-on is not configured for this environment.";
            ConfigureViewFlags();
            return Page();
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }

    private void ConfigureViewFlags()
    {
        SsoEnabled = OpenIdConnectAuthenticationExtensions.IsOpenIdConnectEnabled(_configuration);
        ShowDevCredentials = _environment.IsDevelopment();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }
}
