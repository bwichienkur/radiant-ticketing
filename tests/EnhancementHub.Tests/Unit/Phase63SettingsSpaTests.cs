using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase63SettingsSpaTests
{
    [Fact]
    public void SpaShell_IncludesSettingsRoute()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Settings/*");
        shell.Should().Contain("SettingsApp");
    }

    [Fact]
    public void SpaSettingsController_ExposesBffEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaSettingsController.cs"));
        controller.Should().Contain("[Route(\"web-api/spa/settings\")]");
        controller.Should().Contain("Get(\"authentication\")");
        controller.Should().Contain("Get(\"system\")");
        controller.Should().Contain("Get(\"teams\")");
        controller.Should().Contain("api-keys");
        controller.Should().Contain("webhooks/subscriptions");
    }

    [Fact]
    public void SettingsSpaPage_MountsUnifiedBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/Settings.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa-shell.js");
    }

    [Fact]
    public void LegacyAdminPages_RedirectToSettingsSpa()
    {
        var settings = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Settings.cshtml.cs"));
        var auth = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Authentication.cshtml.cs"));
        var apiKeys = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/ApiKeys.cshtml.cs"));
        var teams = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Teams.cshtml.cs"));
        var webhooks = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Webhooks.cshtml.cs"));

        settings.Should().Contain("RedirectPermanent(\"/Spa/Settings/General\"");
        auth.Should().Contain("RedirectPermanent(\"/Spa/Settings/Authentication\"");
        apiKeys.Should().Contain("RedirectPermanent(\"/Spa/Settings/ApiKeys\"");
        teams.Should().Contain("RedirectPermanent(\"/Spa/Settings/Teams\"");
        webhooks.Should().Contain("RedirectPermanent(\"/Spa/Settings/Webhooks\"");
    }

    [Fact]
    public void SidebarNav_PointsAdminLinkToSettingsSpa()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_SidebarNav.cshtml"));
        nav.Should().Contain("href=\"/Spa/Settings/General\"");
        nav.Should().NotContain("asp-page=\"/Admin/Settings\"");
    }

    [Fact]
    public void SettingsApp_IncludesPrimarySections()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SettingsApp.tsx"));
        app.Should().Contain("SettingsGeneralSection");
        app.Should().Contain("SettingsAuthenticationSection");
        app.Should().Contain("SettingsApiKeysSection");
        app.Should().Contain("SettingsTeamsSection");
        app.Should().Contain("SettingsWebhooksSection");
        app.Should().Contain("readSpaContext");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
