using EnhancementHub.Application.Common;
using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase25UxModernizationTests
{
    [Fact]
    public void CollaborationGroupNames_ForRequest_IsStable()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        CollaborationGroupNames.ForRequest(id)
            .Should().Be("request:11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Accessibility_LayoutContainsSkipLinkAndMainLandmark()
    {
        var layout = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/Pages/Shared/_Layout.cshtml"));

        layout.Should().Contain("skip-link");
        layout.Should().Contain("id=\"main-content\"");
    }

    [Fact]
    public void SpaPilot_HasWebApiEndpoint()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("web-api/spa");
        sources.Should().Contain("GetEnhancementRequestByIdQuery");
    }

    [Fact]
    public void MobileApprovalQueue_HasResponsiveCss()
    {
        var css = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/wwwroot/css/site.css"));

        css.Should().Contain(".approval-queue-list");
        css.Should().Contain(".approval-action-bar");
        css.Should().Contain("prefers-reduced-motion");
    }

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
