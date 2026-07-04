using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnhancementHub.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult?>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IEnhancementHubDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IEnhancementHubDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResult(token, user.Id, user.Email, user.DisplayName, user.Role);
    }
}
