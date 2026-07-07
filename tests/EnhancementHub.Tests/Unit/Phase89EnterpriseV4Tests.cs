using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase89EnterpriseV4Tests
{
    [Fact]
    public void EnterpriseV4Css_ExistsWithDarkTokens()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-enterprise-v4.css"));
        css.Should().Contain("#09090b");
        css.Should().Contain("--eh-primary: #6366f1");
        css.Should().Contain(".eh-header");
        css.Should().Contain(".sidebar-section-toggle");
    }

    [Fact]
    public void Layout_IncludesEnterpriseV4Stylesheet()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));
        layout.Should().Contain("eh-enterprise-v4.css");
        layout.Should().Contain("data-bs-theme=\"dark\"");
    }

    [Fact]
    public void TopBar_IncludesAiHelpAndWorkspace()
    {
        var topbar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AppTopBar.cshtml"));
        topbar.Should().Contain("eh-header");
        topbar.Should().Contain("eh-header-btn--ai");
        topbar.Should().Contain("Default workspace");
        topbar.Should().Contain("data-theme-toggle");
    }

    [Fact]
    public void Sidebar_HasCollapsibleGroups()
    {
        var sidebar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        sidebar.Should().Contain("data-sidebar-group");
        sidebar.Should().Contain("sidebar-section-toggle");
    }

    [Fact]
    public void Theme_DefaultsToDark()
    {
        var theme = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/theme.ts"));
        theme.Should().Contain("return 'Dark'");
        var site = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        site.Should().Contain("return 'dark'");
        site.Should().Contain("initSidebarGroups");
    }

    [Fact]
    public void RequestList_UsesHorizontalFilterToolbar()
    {
        var list = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestListApp.tsx"));
        list.Should().Contain("eh-filter-toolbar");
        list.Should().Contain("eh-filter-toolbar-actions");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
