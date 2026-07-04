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

        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            });

        if (IsOpenIdConnectEnabled(configuration))
        {
            builder.AddOpenIdConnect("oidc", options => ConfigureOpenIdConnect(options, configuration));
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
    }
}
