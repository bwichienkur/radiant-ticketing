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

        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa-shell.js");
        app.Should().Contain("eh-omnibox-cta");
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
        nav.Should().Contain("/Spa/Admin/Tenancy");
    }

    [Fact]
    public void Sidebar_UsesSvgIconPartial()
    {
        var sidebar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        sidebar.Should().Contain("_SidebarIcon");
        sidebar.Should().NotContain("aria-hidden=\"true\">☰");
    }

    [Fact]
    public void UiKit_IncludesConfirmDialogAndPagination()
    {
        var index = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/index.ts"));
        index.Should().Contain("ConfirmDialog");
        index.Should().Contain("Pagination");
    }

    [Fact]
    public void RazorPages_UseSharedPageHeaderPartial()
    {
        var applications = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Applications/Index.cshtml"));
        applications.Should().Contain("_PageHeader");
        applications.Should().Contain("_EmptyState");
    }

    [Fact]
    public void SpaEntries_UseSharedSpUiRoot()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/entries/spa-shell.tsx"));
        shell.Should().Contain("SpUiRoot");
        shell.Should().Contain("SpaShell");
    }

    [Fact]
    public void SpaShell_UsesReactRouter()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("BrowserRouter");
        shell.Should().Contain("/Spa/RequestList");
    }

    [Fact]
    public void Storybook_ConfigExists()
    {
        var main = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/.storybook/main.ts"));
        var stories = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/UIKit.stories.tsx"));
        main.Should().Contain(".stories");
        stories.Should().Contain("PageHeader");
        stories.Should().Contain("StatusBadge");
    }

    [Fact]
    public void RequestListApp_SupportsBulkDecline()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestListApp.tsx"));
        app.Should().Contain("bulkSubmitApprovalActions(pendingIds, 'Reject')");
        app.Should().Contain("Decline selected");
    }

    [Fact]
    public void SpaPages_UseUnifiedShellScript()
    {
        var index = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Index.cshtml"));
        var requestList = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/RequestList.cshtml"));
        index.Should().Contain("spa-shell.js");
        index.Should().Contain("_SpaRoot");
        requestList.Should().Contain("spa-shell.js");
    }

    [Fact]
    public void SpaRequestsController_SupportsPagedListAndExport()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaRequestsController.cs"));
        controller.Should().Contain("pageSize");
        controller.Should().Contain("requests/export");
        controller.Should().Contain("totalCount");
    }

    [Fact]
    public void ListEnhancementRequestsQuery_SupportsPaginationAndIdFilter()
    {
        var query = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/EnhancementRequests/Queries/ListEnhancementRequestsQuery.cs"));
        query.Should().Contain("PagedResult<EnhancementRequestDto>");
        query.Should().Contain("int Page = 1");
        query.Should().Contain("int PageSize = 0");
        query.Should().Contain("IReadOnlyList<Guid>? Ids");
        query.Should().Contain("Skip((page - 1) * pageSize)");
    }

    [Fact]
    public void AdminPages_UseSharedPageHeaderPartial()
    {
        var settings = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Settings.cshtml"));
        var tenancy = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Tenancy.cshtml"));
        var delivery = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Delivery.cshtml"));

        settings.Should().Contain("_PageHeader");
        tenancy.Should().Contain("_PageHeader");
        delivery.Should().Contain("_PageHeader");
        delivery.Should().Contain("eh-section-title");
    }

    [Fact]
    public void SpaApprovalsController_SupportsBulkAction()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaApprovalsController.cs"));
        controller.Should().Contain("bulk-action");
        controller.Should().Contain("BulkSubmitApprovalActionsCommand");
    }

    [Fact]
    public void RequestListApp_SupportsBulkApprove()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestListApp.tsx"));
        app.Should().Contain("bulkSubmitApprovalActions");
        app.Should().Contain("isApprover");
        app.Should().Contain("Approve selected");
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
