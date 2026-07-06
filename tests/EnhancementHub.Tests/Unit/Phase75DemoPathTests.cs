using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase75DemoPathTests
{
    [Fact]
    public void SpaSystemController_ExposesApplicationDetailEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaSystemController.cs"));
        controller.Should().Contain("applications/{id:guid}");
        controller.Should().Contain("GetApplicationProfileQuery");
        controller.Should().Contain("SpaApplicationDetailResponse");
    }

    [Fact]
    public void ApplicationDetailApp_ExistsWithSpaRoutes()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApplicationDetailApp.tsx"));
        app.Should().Contain("getApplicationDetail");
        app.Should().Contain("/Spa/SystemMap?applicationId=");
        app.Should().Contain("/Spa/Repositories");

        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Applications/:id");
        shell.Should().Contain("ApplicationDetailApp");
    }

    [Fact]
    public void NotificationPreferencesApp_UsesSpaBff()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/NotificationPreferencesApp.tsx"));
        app.Should().Contain("getNotificationPreferences");
        app.Should().Contain("updateNotificationPreferences");

        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("/Spa/Account/Notifications");
        shell.Should().Contain("NotificationPreferencesApp");
    }

    [Fact]
    public void ApplicationsApp_LinksToSpaApplicationDetail()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApplicationsApp.tsx"));
        app.Should().Contain("/Spa/Applications/${app.id}");
        app.Should().NotContain("/Applications/Details/");
    }

    [Fact]
    public void LegacyPages_RedirectToSpaDemoPath()
    {
        var applicationDetails = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Applications/Details.cshtml.cs"));
        applicationDetails.Should().Contain("[Obsolete");
        applicationDetails.Should().Contain("RedirectPermanent($\"/Spa/Applications/{id}\")");

        var notificationPreferences = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Account/NotificationPreferences.cshtml.cs"));
        notificationPreferences.Should().Contain("[Obsolete");
        notificationPreferences.Should().Contain("RedirectPermanent(\"/Spa/Account/Notifications\")");
    }

    [Fact]
    public void TopBarAndSpaPrefixes_IncludeNotificationPreferencesRoute()
    {
        var topBar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AppTopBar.cshtml"));
        topBar.Should().Contain("/Spa/Account/Notifications");

        var spaRoutes = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/spaRoutes.ts"));
        spaRoutes.Should().Contain("/Spa/Account/Notifications");

        var siteJs = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/js/site.js"));
        siteJs.Should().Contain("/Spa/Account/Notifications");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
