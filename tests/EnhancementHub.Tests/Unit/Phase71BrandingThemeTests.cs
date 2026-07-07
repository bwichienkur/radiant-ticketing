using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase71BrandingThemeTests
{
    [Fact]
    public void SpaBrandingController_ExposesAppearanceEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaBrandingController.cs"));
        controller.Should().Contain("[Route(\"web-api/spa/branding\")]");
        controller.Should().Contain("Get(\"appearance\")");
        controller.Should().Contain("Put(\"theme\")");
        controller.Should().Contain("Put(\"tenant\")");
        controller.Should().Contain("GetUserAppearanceQuery");
        controller.Should().Contain("UpdateThemePreferenceCommand");
        controller.Should().Contain("UpdateTenantBrandingCommand");
    }

    [Fact]
    public void TenantBrandingEntity_ConfiguredWithTenantRelationship()
    {
        var config = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Persistence/Configurations/TenantBrandingConfiguration.cs"));
        config.Should().Contain("TenantBrandings");
        config.Should().Contain("HasIndex(x => x.TenantId).IsUnique()");
    }

    [Fact]
    public void UserEntity_IncludesThemePreference()
    {
        var user = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/User.cs"));
        user.Should().Contain("ThemePreference ThemePreference");
    }

    [Fact]
    public void SettingsApp_IncludesBrandingSection()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SettingsApp.tsx"));
        app.Should().Contain("SettingsBrandingSection");
        app.Should().Contain("path=\"Branding\"");
    }

    [Fact]
    public void SpaShell_LoadsAppearanceAndThemeSelector()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("SpaAppearanceBootstrap");
        shell.Should().Contain("ThemePreferenceSelector");
        shell.Should().Contain("applyTenantBranding");
    }

    [Fact]
    public void ThemeModule_SupportsSystemLightDark()
    {
        var theme = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/theme.ts"));
        theme.Should().Contain("'System' | 'Light' | 'Dark'");
        theme.Should().Contain("applyThemePreference");
        theme.Should().Contain("applyTenantBranding");
    }

    [Fact]
    public void SiteJs_ThemeInitSupportsSystemPreference()
    {
        var site = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        site.Should().Contain("resolveThemePreference");
        site.Should().Contain("'Light'");
        site.Should().Contain("'Dark'");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
