using System.Security.Claims;
using EnhancementHub.Application.Auth;
using EnhancementHub.Application.Common.Models;
using MediatR;

namespace EnhancementHub.Web.Services;

public sealed class DevAuthService
{
    private readonly IMediator _mediator;

    public DevAuthService(IMediator mediator) => _mediator = mediator;

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new LoginCommand(email, password), cancellationToken);
    }

    public static ClaimsPrincipal CreatePrincipal(LoginResult login)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.UserId.ToString()),
            new(ClaimTypes.Email, login.Email),
            new(ClaimTypes.Name, login.DisplayName),
            new(ClaimTypes.Role, login.Role.ToString())
        };

        if (login.TenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", login.TenantId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }
}
