using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase76AdminSpaTests
{
    [Fact]
    public void AdminApp_ExistsWithCoreRoutes()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/AdminApp.tsx"));
        app.Should().Contain("AdminJobsSection");
        app.Should().Contain("AdminComplianceSection");
        app.Should().Contain("AdminCustomFieldsSection");
        app.Should().Contain("path=\"Jobs\"");
    }

    [Fact]
    public void SpaAdminController_ExposesJobsComplianceAndCustomFields()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaAdminController.cs"));
        controller.Should().Contain("web-api/spa/admin");
        controller.Should().Contain("jobs");
        controller.Should().Contain("compliance/soc2");
        controller.Should().Contain("custom-fields");
    }

    [Fact]
    public void AdminNav_ReplacesLegacyRazorLinks()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/settings/SettingsNav.tsx"));
        nav.Should().Contain("/Spa/Admin/Jobs");
        nav.Should().NotContain("href=\"/Admin/Jobs\"");
        nav.Should().NotContain("asp-page=\"/Admin/Jobs\"");
    }

    [Fact]
    public void JobsRazorPage_RedirectsToSpaAdmin()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Jobs.cshtml.cs"));
        page.Should().Contain("[Obsolete");
        page.Should().Contain("/Spa/Admin/Jobs");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
