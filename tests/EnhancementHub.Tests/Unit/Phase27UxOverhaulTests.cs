using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase27UxOverhaulTests
{
    [Fact]
    public void Layout_UsesSidebarAppShell()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));
        layout.Should().Contain("app-shell");
        layout.Should().Contain("_SidebarNav");
        layout.Should().Contain("_AppTopBar");
        layout.Should().Contain("site.js");
    }

    [Fact]
    public void SiteCss_DefinesSidebarAndCommandPalette()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/site.css"));
        css.Should().Contain(".app-sidebar");
        css.Should().Contain(".command-palette");
        css.Should().Contain("--bs-primary");
    }

    [Fact]
    public void Dashboard_HasCopilotBarAndActivityFeed()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/DashboardApp.tsx"));
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Index.cshtml"));

        page.Should().Contain("spa-dashboard-root");
        app.Should().Contain("copilot-bar");
        app.Should().Contain("Recent activity");
        app.Should().Contain("sparkline");
    }

    [Fact]
    public void RequestList_HasSearchAndFilterChips()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/EnhancementRequests/Index.cshtml"));
        page.Should().Contain("filter-chips");
        page.Should().Contain("request-card-mobile");
        page.Should().Contain("empty-state");
    }

    [Fact]
    public void ApprovalQueue_HasQuickActionsAndRiskBadges()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/EnhancementRequests/Approve.cshtml"));
        page.Should().Contain("approval-decision-header");
        page.Should().Contain("approval-quick-actions");
        page.Should().Contain("d waiting");
    }

    [Fact]
    public void RequestDetail_HasMissionControlAndCommentForm()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/EnhancementRequests/Details.cshtml"));
        page.Should().Contain("Mission control");
        page.Should().Contain("asp-page-handler=\"Comment\"");
        page.Should().Contain("data-eh-accordion");
    }

    [Fact]
    public void Login_LinksToSignupTrial()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Account/Login.cshtml"));
        page.Should().Contain("Start your free trial");
    }

    [Fact]
    public void UxController_ExposesSearchAndCopilot()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/UxController.cs"));
        controller.Should().Contain("[Route(\"web-api/ux\")]");
        controller.Should().Contain("search");
        controller.Should().Contain("copilot");
    }

    [Fact]
    public void AdminNav_IncludesTenancy()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AdminNav.cshtml"));
        nav.Should().Contain("/Admin/Tenancy");
    }

    private static string GetPath(string relative) =>
        Path.Combine(GetRepoRoot(), relative);

    private static string GetRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "EnhancementHub.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException("Repo root not found");
    }
}
