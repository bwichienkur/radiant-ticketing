using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase73CommandPaletteTests
{
    [Fact]
    public void AppTopBar_DoesNotIncludeLegacyCommandPaletteModal()
    {
        var topBar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AppTopBar.cshtml"));
        topBar.Should().NotContain("id=\"commandPalette\"");
        topBar.Should().NotContain("commandPaletteInput");
        topBar.Should().Contain("eh-topbar-theme-slot");
        topBar.Should().Contain("data-theme-toggle");
        topBar.Should().Contain("data-command-trigger");
    }

    [Fact]
    public void SiteJs_SkipsLegacyPaletteWhenSpaRootPresent()
    {
        var site = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        site.Should().Contain("isSpaShell");
        site.Should().Contain("if (!isSpaShell)");
        site.Should().Contain("initCommandPalette()");
    }

    [Fact]
    public void ThemePreferenceSelector_UsesTopBarPortal()
    {
        var selector = File.ReadAllText(GetPath(
            "src/EnhancementHub.Web/ClientApp/src/components/ThemePreferenceSelector.tsx"));
        selector.Should().Contain("eh-topbar-theme-slot");
        selector.Should().Contain("createPortal");
    }

    [Fact]
    public void Layout_AppliesThemeBeforePaint()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));
        layout.Should().Contain("localStorage.getItem('eh-theme')");
        layout.Should().Contain("data-bs-theme");
    }

    [Fact]
    public void ResponsiveDataList_ComponentExists()
    {
        var component = File.ReadAllText(GetPath(
            "src/EnhancementHub.Web/ClientApp/src/components/ui/ResponsiveDataList.tsx"));
        component.Should().Contain("table-desktop-only");
        component.Should().Contain("cards-mobile-only");
    }

    [Fact]
    public void ApplicationsApp_UsesResponsiveDataList()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApplicationsApp.tsx"));
        app.Should().Contain("ResponsiveDataList");
        app.Should().NotContain("/Applications/Details/");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
