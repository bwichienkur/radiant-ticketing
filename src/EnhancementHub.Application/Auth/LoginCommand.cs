using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Persistence;
using EnhancementHub.Application.Common.Models;
using MediatR;

namespace EnhancementHub.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult?>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FindActiveByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResult(token, user.Id, user.Email, user.DisplayName, user.Role, user.TenantId);
    }
}
