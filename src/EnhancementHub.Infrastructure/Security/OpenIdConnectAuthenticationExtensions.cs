using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EnhancementHub.Infrastructure.Security;

public static class OpenIdConnectAuthenticationExtensions
{
    public static bool IsOpenIdConnectEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Authentication:OpenIdConnect:Enabled");

    public static AuthenticationBuilder AddEnhancementHubJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? "dev-secret-change-in-production-min-32-chars!!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "EnhancementHub";
        var jwtAudience = configuration["Jwt:Audience"] ?? "EnhancementHub";

        var builder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = EnhancementHubAuthDefaults.PolicyScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(EnhancementHubAuthDefaults.PolicyScheme, "JWT or API Key", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/scim", StringComparison.OrdinalIgnoreCase))
                    {
                        return ScimAuthenticationDefaults.Scheme;
                    }

                    if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationDefaults.HeaderName))
                    {
                        return ApiKeyAuthenticationDefaults.Scheme;
                    }

                    return JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSecret))
                };
                configureJwt?.Invoke(options);
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.Scheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, ScimBearerAuthenticationHandler>(
                ScimAuthenticationDefaults.Scheme,
                _ => { });

        if (IsOpenIdConnectEnabled(configuration))
        {
            builder.AddOpenIdConnect(options => ConfigureOpenIdConnect(options, configuration));
        }

        return builder;
    }

    public static AuthenticationBuilder AddEnhancementHubCookieAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                if (IsOpenIdConnectEnabled(configuration))
                {
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });

        if (IsOpenIdConnectEnabled(configuration))
        {
            builder.AddOpenIdConnect(options => ConfigureOpenIdConnect(options, configuration));
        }

        return builder;
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options, IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication:OpenIdConnect");
        options.Authority = section["Authority"];
        options.ClientId = section["ClientId"];
        options.ClientSecret = section["ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.CallbackPath = section["CallbackPath"] ?? "/signin-oidc";
        options.SignedOutCallbackPath = section["SignedOutCallbackPath"] ?? "/signout-callback-oidc";

        var scopes = section.GetSection("Scopes").Get<string[]>();
        options.Scope.Clear();
        if (scopes is { Length: > 0 })
        {
            foreach (var scope in scopes)
            {
                options.Scope.Add(scope);
            }
        }
        else
        {
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
        }

        options.MapInboundClaims = false;
        options.UsePkce = true;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
        options.TokenValidationParameters.ValidateIssuer = true;

        var roleMappings = section.GetSection("RoleMappings").Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                return Task.CompletedTask;
            }

            MapEntraIdentityClaims(identity);

            var mappedSources = identity.FindAll("groups")
                .Concat(identity.FindAll("roles"))
                .Concat(identity.FindAll(ClaimTypes.Role))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var source in mappedSources)
            {
                if (roleMappings.TryGetValue(source, out var mappedRole))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, mappedRole));
                }
            }

            var defaultRole = section["DefaultRole"];
            if (!string.IsNullOrWhiteSpace(defaultRole) && !identity.HasClaim(c => c.Type == ClaimTypes.Role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, defaultRole));
            }

            return Task.CompletedTask;
        };
    }

    private static void MapEntraIdentityClaims(ClaimsIdentity identity)
    {
        var objectId = identity.FindFirst("oid")?.Value ?? identity.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(objectId)
            && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, objectId));
        }

        var email = identity.FindFirst("email")?.Value
            ?? identity.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrWhiteSpace(email) && !identity.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        var displayName = identity.FindFirst("name")?.Value;
        if (!string.IsNullOrWhiteSpace(displayName) && !identity.HasClaim(c => c.Type == ClaimTypes.Name))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
        }

        foreach (var appRole in identity.FindAll("roles"))
        {
            if (!identity.HasClaim(ClaimTypes.Role, appRole.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, appRole.Value));
            }
        }
    }
}
