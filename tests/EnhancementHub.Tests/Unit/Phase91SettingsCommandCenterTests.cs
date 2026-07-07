using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase91SettingsCommandCenterTests
{
    [Fact]
    public void SettingsCommandCenterCss_Exists()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-settings-command-center.css"));
        css.Should().Contain(".eh-settings-command-center");
        css.Should().Contain(".eh-settings-category-card");
        css.Should().Contain(".eh-settings-sidebar__link--active");
    }

    [Fact]
    public void Layout_IncludesSettingsCommandCenterStylesheet()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));
        layout.Should().Contain("eh-settings-command-center.css");
    }

    [Fact]
    public void SettingsCatalog_DefinesCategoriesAndSections()
    {
        var catalog = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/settings/settingsCatalog.ts"));
        catalog.Should().Contain("SETTINGS_CATEGORIES");
        catalog.Should().Contain("SETTINGS_SECTIONS");
        catalog.Should().Contain("searchSettings");
    }

    [Fact]
    public void SettingsApp_UsesCommandCenterHub()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SettingsApp.tsx"));
        app.Should().Contain("SettingsCommandCenter");
        app.Should().Contain("SettingsHubRoute");
        app.Should().Contain("SettingsSectionPage");
        app.Should().NotContain("PageHeader");
    }

    [Fact]
    public void AdminApp_UsesSettingsSectionShell()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/AdminApp.tsx"));
        app.Should().Contain("SettingsCommandCenter");
        app.Should().Contain("SettingsSectionPage");
    }

    [Fact]
    public void Sidebar_LinksToSettingsHub()
    {
        var sidebar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        sidebar.Should().Contain("href=\"/Spa/Settings\"");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
