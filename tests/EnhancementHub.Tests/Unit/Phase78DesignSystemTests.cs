using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase78DesignSystemTests
{
    [Fact]
    public void ClientApp_NoRawBootstrapAlertsOutsideUIKit()
    {
        var srcDir = GetPath("src/EnhancementHub.Web/ClientApp/src");
        var offenders = Directory
            .GetFiles(srcDir, "*.tsx", SearchOption.AllDirectories)
            .Where(path => !path.Contains("AlertBanner.tsx", StringComparison.Ordinal))
            .Select(path => (path, content: File.ReadAllText(path)))
            .Where(pair => pair.content.Contains("alert alert-", StringComparison.Ordinal))
            .Select(pair => Path.GetRelativePath(GetRepoRoot(), pair.path))
            .ToList();

        offenders.Should().BeEmpty($"raw Bootstrap alerts should use AlertBanner: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void AlertBanner_UsesAlertRoleForDangerAndWarning()
    {
        var banner = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/ui/AlertBanner.tsx"));
        banner.Should().Contain("variant === 'danger'");
        banner.Should().Contain("role={role}");
    }

    [Fact]
    public void SpaShell_AnnouncesRouteChangesToLiveRegion()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("eh-spa-live-region");
    }

    [Fact]
    public void Vitest_ConfigAndScriptsExist()
    {
        var packageJson = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/package.json"));
        packageJson.Should().Contain("\"test\"");
        packageJson.Should().Contain("vitest");
        File.Exists(GetPath("src/EnhancementHub.Web/ClientApp/vitest.config.ts")).Should().BeTrue();
    }

    [Fact]
    public void Storybook_IncludesA11yAddonAndComponentStories()
    {
        var main = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/.storybook/main.ts"));
        main.Should().Contain("addon-a11y");
        File.Exists(GetPath("src/EnhancementHub.Web/ClientApp/src/components/CommandPalette.stories.tsx")).Should().BeTrue();
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));

    private static string GetRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
