using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase90EdgePolishTests
{
    [Fact]
    public void EnterpriseV4Css_HasTableAndStatPolish()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-enterprise-v4.css"));
        css.Should().Contain(".eh-table-checkbox-col");
        css.Should().Contain(".eh-table-badge-col");
        css.Should().Contain(".eh-topbar-theme.topbar-theme-slot");
        css.Should().Contain("display: none !important");
        css.Should().Contain(".stat-card .value.text-primary");
    }

    [Fact]
    public void TopBar_UsesConciseSearchPlaceholder()
    {
        var topbar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AppTopBar.cshtml"));
        topbar.Should().Contain("Search workspace");
        topbar.Should().NotContain("Search requests, apps, pages");
    }

    [Fact]
    public void ThemePreferenceSelector_OmitsThemeLabel()
    {
        var selector = File.ReadAllText(GetPath(
            "src/EnhancementHub.Web/ClientApp/src/components/ThemePreferenceSelector.tsx"));
        selector.Should().NotContain(">Theme</span>");
        selector.Should().Contain("SegmentedControl");
    }

    [Fact]
    public void RequestList_AlignsCheckboxAndStatusColumns()
    {
        var list = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestListApp.tsx"));
        list.Should().Contain("eh-table-checkbox-col");
        list.Should().Contain("eh-table-badge-col");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
