using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase33ReactCollaborationTests
{
    [Fact]
    public void ClientApp_IncludesSignalRDependency()
    {
        var packageJson = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/package.json"));
        packageJson.Should().Contain("@microsoft/signalr");
    }

    [Fact]
    public void RequestDetailApp_UsesCollaborationHook()
    {
        var app = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestDetailApp.tsx"));
        app.Should().Contain("useRequestCollaboration");
        app.Should().Contain("postRequestComment");
        app.Should().Contain("collaboration-live-comments");
    }

    [Fact]
    public void SpaBff_ExposesCommentBff()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("requests/{id:guid}/comments");
        sources.Should().Contain("AddCommentCommand");
    }

    [Fact]
    public void TriggerAiAnalysis_NotifiesCollaborationHub()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Analysis/Commands/TriggerAiAnalysisCommand.cs"));
        handler.Should().Contain("NotifyAnalysisUpdatedAsync");
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
