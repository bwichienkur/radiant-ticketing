using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase35LoadTestHarnessTests
{
    [Fact]
    public void LoadTestDocs_DocumentsHorizon3Criteria()
    {
        var doc = File.ReadAllText(GetPath("docs/LOAD_TEST.md"));
        doc.Should().Contain("200");
        doc.Should().Contain("500");
        doc.Should().Contain("50");
    }

    [Fact]
    public void K6SmokeScript_Exists()
    {
        var script = File.ReadAllText(GetPath("tests/load/k6-smoke.js"));
        script.Should().Contain("/health");
        script.Should().Contain("/api/v1/EnhancementRequests");
    }

    [Fact]
    public void K6Horizon3Script_HasStagedLoad()
    {
        var script = File.ReadAllText(GetPath("tests/load/k6-horizon3.js"));
        script.Should().Contain("stages");
        script.Should().Contain("target: 500");
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
