using FluentAssertions;

namespace EnhancementHub.Tests.Integration;

public sealed class SpaBffTests
{
    [Fact]
    public void SpaAdminController_ExposesCoreBffRoutes()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaAdminController.cs"));
        controller.Should().Contain("[Route(\"web-api/spa/admin\")]");
        controller.Should().Contain("HttpGet(\"jobs\")");
        controller.Should().Contain("HttpGet(\"compliance/soc2\")");
        controller.Should().Contain("HttpGet(\"custom-fields\")");
        controller.Should().Contain("HttpGet(\"tenancy\")");
    }

    [Fact]
    public void SpaPortfolioController_ExposesHealthAndExport()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaPortfolioController.cs"));
        controller.Should().Contain("HttpGet(\"health\")");
        controller.Should().Contain("HttpGet(\"health/export\")");
    }

    [Fact]
    public void SpaNotificationsController_ExposesPreferencesRoutes()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaNotificationsController.cs"));
        controller.Should().Contain("HttpGet(\"preferences\")");
        controller.Should().Contain("HttpPut(\"preferences\")");
    }

    [Fact]
    public void SpaSystemController_ExposesApplicationDetailRoute()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaSystemController.cs"));
        controller.Should().Contain("applications/{id:guid}");
    }

    [Fact]
    public void SpaClient_IncludesAdminAndPortfolioHelpers()
    {
        var client = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/api/spaClient.ts"));
        client.Should().Contain("getAdminJobs");
        client.Should().Contain("getPortfolioHealthExportUrl");
        client.Should().Contain("getNotificationPreferences");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
