namespace EnhancementHub.Application.Features.Branding.Dtos;

public sealed record TenantBrandingDto(
    string? LogoUrl,
    string AccentColor,
    string? ProductName);

public sealed record UserAppearanceDto(
    string ThemePreference,
    TenantBrandingDto Branding);
