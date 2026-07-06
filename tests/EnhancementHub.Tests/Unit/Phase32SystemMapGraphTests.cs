using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase32SystemMapGraphTests
{
    [Fact]
    public void ClientApp_IncludesCytoscapeDependency()
    {
        var packageJson = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/package.json"));
        packageJson.Should().Contain("\"cytoscape\"");
    }

    [Fact]
    public void SystemMapGraphComponent_UsesCytoscapeLayout()
    {
        var component = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SystemMapGraph.tsx"));
        component.Should().Contain("cytoscape");
        component.Should().Contain("name: 'cose'");
        component.Should().Contain("system-map-graph-canvas");
    }

    [Fact]
    public void SystemMapGraphHelpers_CapNodesAndStyleByType()
    {
        var helpers = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/systemMapGraph.ts"));
        helpers.Should().Contain("MAX_GRAPH_NODES = 400");
        helpers.Should().Contain("buildCytoscapeElements");
        helpers.Should().Contain("buildCytoscapeStyles");
        helpers.Should().Contain("NODE_COLORS");
        helpers.Should().Contain("node[type = \"${type}\"]");
    }

    [Fact]
    public void SystemMapApp_HasGraphAndListToggle()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SystemMapApp.tsx"));
        app.Should().Contain("SystemMapGraph");
        app.Should().Contain("viewMode === 'graph'");
        app.Should().Contain("onNodeSelected={setSelectedNodeId}");
    }

    [Fact]
    public void SiteCss_StylesSystemMapGraphCanvas()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/site.css"));
        css.Should().Contain(".system-map-graph-canvas");
    }

    [Fact]
    public void ReactSystemMapBundle_IsPublishedToWwwroot()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/wwwroot/spa/react/spa-shell.js")).Should().BeTrue();
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("SystemMapApp");
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
