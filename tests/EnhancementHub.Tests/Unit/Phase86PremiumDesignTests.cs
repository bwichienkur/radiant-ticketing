using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase86PremiumDesignTests
{
    [Fact]
    public void PremiumDesignLanguageDoc_Exists()
    {
        var doc = File.ReadAllText(GetPath("docs/PREMIUM_DESIGN_LANGUAGE.md"));
        doc.Should().Contain("Quiet confidence");
        doc.Should().Contain("Linear");
        doc.Should().Contain("Token architecture");
    }

    [Fact]
    public void PremiumCss_IncludesDesignTokensAndShell()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-premium-v3.css"));
        css.Should().Contain("--eh-accent:");
        css.Should().Contain(".app-sidebar");
        css.Should().Contain(".command-palette-backdrop");
        css.Should().Contain(".eh-skeleton");
        css.Should().Contain("prefers-reduced-motion");
    }

    [Fact]
    public void Layout_IncludesPremiumStylesheet()
    {
        var layout = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));
        var login = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_LoginLayout.cshtml"));
        layout.Should().Contain("eh-premium-v3.css");
        login.Should().Contain("eh-premium-v3.css");
    }

    [Fact]
    public void LoadingSkeleton_UsesPremiumShimmer()
    {
        var skeleton = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/LoadingSkeleton.tsx"));
        skeleton.Should().Contain("eh-skeleton");
        skeleton.Should().Contain("eh-skeleton-line");
    }

    [Fact]
    public void StatusBadge_UsesPremiumChipClass()
    {
        var badge = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/StatusBadge.tsx"));
        badge.Should().Contain("eh-status-chip");
    }

    [Fact]
    public void PortfolioSpaPage_Exists()
    {
        File.Exists(GetPath("src/EnhancementHub.Web/Pages/Spa/Portfolio.cshtml")).Should().BeTrue();
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Spa/Portfolio.cshtml"));
        page.Should().Contain("/Spa/Portfolio");
        page.Should().Contain("_SpaRoot");
    }

    [Fact]
    public void EmptyState_UsesPremiumClass()
    {
        var empty = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/EmptyState.tsx"));
        empty.Should().Contain("eh-empty-state");
    }

    [Fact]
    public void CommandPalette_UsesPremiumBackdropClass()
    {
        var palette = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/CommandPalette.tsx"));
        palette.Should().Contain("eh-command-palette");
    }

    [Fact]
    public void LoadingState_UsesPremiumWrapper()
    {
        var loading = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/LoadingState.tsx"));
        loading.Should().Contain("eh-loading-state");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
