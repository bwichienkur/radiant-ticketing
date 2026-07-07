using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase88SurfacePolishTests
{
    [Fact]
    public void TopBar_UsesModernChromeMarkup()
    {
        var topbar = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AppTopBar.cshtml"));
        topbar.Should().Contain("eh-topbar");
        topbar.Should().Contain("eh-topbar-search");
        topbar.Should().Contain("eh-topbar-avatar");
        topbar.Should().Contain("eh-dropdown-panel");
        topbar.Should().NotContain("btn-outline-secondary btn-sm dropdown-toggle");
    }

    [Fact]
    public void Login_UsesStructuredFormFields()
    {
        var login = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Account/Login.cshtml"));
        login.Should().Contain("login-form");
        login.Should().Contain("login-field");
        login.Should().Contain("login-submit-wrap");
        login.Should().NotContain("form-control-lg");
    }

    [Fact]
    public void PremiumCss_IncludesTopbarLoginAndCommandPalettePolish()
    {
        var css = File.ReadAllText(GetPath("src/EnhancementHub.Web/wwwroot/css/eh-premium-v3.css"));
        css.Should().Contain(".eh-topbar-search");
        css.Should().Contain(".eh-command-palette-modal");
        css.Should().Contain(".eh-topbar-dropdown .dropdown-menu");
        css.Should().Contain(".login-submit-btn");
    }

    [Fact]
    public void CommandPalette_UsesModernResultLayout()
    {
        var palette = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/CommandPalette.tsx"));
        palette.Should().Contain("eh-command-result");
        palette.Should().Contain("eh-command-palette-shortcuts");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
