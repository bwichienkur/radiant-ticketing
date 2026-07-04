using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.Admin.Dtos;
using EnhancementHub.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace EnhancementHub.Infrastructure.Services;

public sealed class AuthenticationConfigurationService : IAuthenticationConfigurationService
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(UserRole.Admin),
        nameof(UserRole.Approver),
        nameof(UserRole.Developer),
        nameof(UserRole.Reviewer),
        nameof(UserRole.Submitter)
    };

    private readonly IConfiguration _configuration;

    public AuthenticationConfigurationService(IConfiguration configuration) => _configuration = configuration;

    public AuthenticationConfigurationStatusDto GetStatus()
    {
        var section = _configuration.GetSection("Authentication:OpenIdConnect");
        var enabled = section.GetValue<bool>("Enabled");
        var issues = new List<ConfigurationValidationIssueDto>();
        var roleMappings = BuildRoleMappingValidations(section, issues);

        var authority = section["Authority"];
        var clientId = section["ClientId"];
        var clientSecret = section["ClientSecret"];
        var defaultRole = section["DefaultRole"];
        var scopes = section.GetSection("Scopes").Get<string[]>()?.ToList()
            ?? ["openid", "profile", "email"];

        if (enabled)
        {
            ValidateRequired(section, "Authority", issues);
            ValidateRequired(section, "ClientId", issues);
            ValidateRequired(section, "ClientSecret", issues);

            if (!string.IsNullOrWhiteSpace(authority)
                && (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
                    || authorityUri.Scheme is not ("http" or "https")))
            {
                issues.Add(new("Error", "Authority must be a valid absolute URL."));
            }

            if (string.IsNullOrWhiteSpace(defaultRole) && roleMappings.Count == 0)
            {
                issues.Add(new("Error", "Configure DefaultRole or at least one RoleMappings entry."));
            }

            if (!string.IsNullOrWhiteSpace(defaultRole) && !ValidRoles.Contains(defaultRole))
            {
                issues.Add(new("Error", $"DefaultRole '{defaultRole}' is not a recognized application role."));
            }

            if (!scopes.Contains("openid", StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(new("Warning", "Scopes should include 'openid' for Entra ID sign-in."));
            }
        }
        else
        {
            issues.Add(new("Info", "OpenID Connect is disabled. Users sign in with local credentials."));
        }

        var isProductionReady = enabled
            && !issues.Any(i => i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));

        return new AuthenticationConfigurationStatusDto(
            enabled,
            isProductionReady,
            authority,
            clientId,
            !string.IsNullOrWhiteSpace(clientSecret),
            defaultRole,
            scopes,
            roleMappings,
            issues);
    }

    private static List<RoleMappingValidationDto> BuildRoleMappingValidations(
        IConfiguration section,
        List<ConfigurationValidationIssueDto> issues)
    {
        var mappings = section.GetSection("RoleMappings").Get<Dictionary<string, string>>() ?? [];
        var results = new List<RoleMappingValidationDto>();

        foreach (var (source, targetRole) in mappings)
        {
            var isValidRole = ValidRoles.Contains(targetRole);
            var isGuid = Guid.TryParse(source, out _);

            if (string.IsNullOrWhiteSpace(source))
            {
                issues.Add(new("Error", "RoleMappings contains an empty source key."));
            }

            if (!isValidRole)
            {
                issues.Add(new("Error", $"Role mapping '{source}' → '{targetRole}' uses an unrecognized role."));
            }
            else if (!isGuid)
            {
                issues.Add(new("Warning", $"Role mapping source '{source}' is not a GUID — verify this matches your Entra group or app role identifier."));
            }

            results.Add(new RoleMappingValidationDto(source, targetRole, isValidRole, isGuid));
        }

        return results;
    }

    private static void ValidateRequired(IConfiguration section, string key, List<ConfigurationValidationIssueDto> issues)
    {
        if (string.IsNullOrWhiteSpace(section[key]))
        {
            issues.Add(new("Error", $"{key} is required when OpenID Connect is enabled."));
        }
    }
}
