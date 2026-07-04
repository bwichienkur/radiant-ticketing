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

    public static ClaimsPrincipal CreatePrincipal(LoginResult login) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, login.UserId.ToString()),
            new Claim(ClaimTypes.Email, login.Email),
            new Claim(ClaimTypes.Name, login.DisplayName),
            new Claim(ClaimTypes.Role, login.Role.ToString())
        }, "Cookies"));
}
