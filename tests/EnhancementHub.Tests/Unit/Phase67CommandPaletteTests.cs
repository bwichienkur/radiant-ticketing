using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase67CommandPaletteTests
{
    [Fact]
    public void SpaShell_IncludesCommandPalette()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("CommandPalette");
    }

    [Fact]
    public void CommandPalette_UsesSemanticGroupedSearch()
    {
        var palette = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/CommandPalette.tsx"));
        palette.Should().Contain("searchGlobalGrouped");
        palette.Should().Contain("metaKey");
        palette.Should().Contain("role=\"dialog\"");
        palette.Should().Contain("semanticHint");
    }

    [Fact]
    public void GlobalEntitySearchQuery_SupportsSemanticMode()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Search/Queries/GlobalEntitySearchQuery.cs"));
        handler.Should().Contain("bool Semantic");
        handler.Should().Contain("FeatureFlags.SemanticSearch");
        handler.Should().Contain("BuildSemanticHint");
    }

    [Fact]
    public void SpaSearchController_AcceptsSemanticParameter()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaSearchController.cs"));
        controller.Should().Contain("semantic");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
