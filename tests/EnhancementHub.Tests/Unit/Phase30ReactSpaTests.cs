using EnhancementHub.Tests.Common;
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
        viteConfig.Should().Contain("spa-shell");
    }

    [Fact]
    public void ReactBundles_ArePublishedToWwwroot()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/spa-shell.js")).Should().BeTrue();
        new FileInfo(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/spa-shell.js")).Length.Should().BeGreaterThan(100_000);
    }

    [Fact]
    public void SpaBff_ExposesReactBffEndpoints()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("web-api/spa");
        sources.Should().Contain("ListApplicationsQuery");
        sources.Should().Contain("GetSystemMapQuery");
    }

    [Fact]
    public void RequestDetailSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/RequestDetail.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa/react/spa-shell.js");
    }

    [Fact]
    public void SystemMapSpaPage_MountsReactBundle()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/SystemMap.cshtml"));
        page.Should().Contain("_SpaRoot");
        page.Should().Contain("spa/react/spa-shell.js");
    }

    [Fact]
    public void WebProject_BuildsClientAppOnCompile()
    {
        var csproj = File.ReadAllText(GetPath("src/EnhancementHub.Web/EnhancementHub.Web.csproj"));
        csproj.Should().Contain("BuildClientApp");
        csproj.Should().Contain("ClientApp");
    }

    [Fact]
    public void SpaBff_RequiresAuthorization()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("[Authorize]");
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
