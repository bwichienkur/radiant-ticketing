using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase30ReactSpaTests
{
    [Fact]
    public void ClientApp_HasViteReactToolchain()
    {
        var packageJson = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/package.json"));
        var viteConfig = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/vite.config.ts"));
        packageJson.Should().Contain("\"react\"");
        packageJson.Should().Contain("\"vite\"");
        viteConfig.Should().Contain("request-detail");
        viteConfig.Should().Contain("system-map");
    }

    [Fact]
    public void ReactBundles_ArePublishedToWwwroot()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/request-detail.js")).Should().BeTrue();
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/system-map.js")).Should().BeTrue();
    }

    [Fact]
    public void SpaDataController_ExposesReactBffEndpoints()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/SpaDataController.cs"));
        controller.Should().Contain("web-api/spa");
        controller.Should().Contain("ListApplicationsQuery");
        controller.Should().Contain("GetSystemMapQuery");
    }

    [Fact]
    public void RequestDetailSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/RequestDetail.cshtml"));
        page.Should().Contain("spa-request-detail-root");
        page.Should().Contain("spa/react/request-detail.js");
        page.Should().NotContain("~/js/spa/request-detail.js");
    }

    [Fact]
    public void SystemMapSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/SystemMap.cshtml"));
        page.Should().Contain("spa-system-map-root");
        page.Should().Contain("spa/react/system-map.js");
    }

    [Fact]
    public void WebProject_BuildsClientAppOnCompile()
    {
        var csproj = File.ReadAllText(GetPath("src/EnhancementHub.Web/EnhancementHub.Web.csproj"));
        csproj.Should().Contain("BuildClientApp");
        csproj.Should().Contain("ClientApp");
    }

    [Fact]
    public void SpaDataController_RequiresAuthorization()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/SpaDataController.cs"));
        controller.Should().Contain("[Authorize]");
    }

    private static string GetPath(string relativePath) =>
        Path.Combine(GetRepoRoot(), relativePath);

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
