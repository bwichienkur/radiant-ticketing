namespace EnhancementHub.Application.Features.Admin.Dtos;

public sealed record AuthenticationConfigurationStatusDto(
    bool OpenIdConnectEnabled,
    bool IsProductionReady,
    string? Authority,
    string? ClientId,
    bool ClientSecretConfigured,
    string? DefaultRole,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<RoleMappingValidationDto> RoleMappings,
    IReadOnlyList<ConfigurationValidationIssueDto> Issues);

public sealed record RoleMappingValidationDto(
    string Source,
    string TargetRole,
    bool IsValidTargetRole,
    bool IsGuidFormat);

public sealed record ConfigurationValidationIssueDto(
    string Severity,
    string Message);
