using System.Security.Claims;
using System.Text.Encodings.Web;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnhancementHub.Infrastructure.Security;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly EnhancementHubDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        EnhancementHubDbContext dbContext,
        IPasswordHasher passwordHasher)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues)
            || string.IsNullOrWhiteSpace(headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = headerValues.ToString().Trim();
        if (!apiKey.StartsWith(ApiKeyAuthenticationDefaults.KeyPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        var prefix = apiKey.Length >= 11
            ? apiKey[..11]
            : apiKey;

        var candidates = await _dbContext.ServiceApiKeys
            .Include(k => k.ServiceUser)
            .Where(k => k.IsActive && k.KeyPrefix == prefix)
            .ToListAsync(Context.RequestAborted);

        var match = candidates.FirstOrDefault(k =>
            _passwordHasher.Verify(apiKey, k.KeyHash)
            && k.ServiceUser.IsActive
            && (k.ExpiresAt is null || k.ExpiresAt > DateTime.UtcNow));

        if (match is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        match.LastUsedAt = DateTime.UtcNow;
        match.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(Context.RequestAborted);

        var user = match.ServiceUser;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("auth_method", "api_key"),
            new Claim("api_key_id", match.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
