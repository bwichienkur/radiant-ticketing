using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase55LoadTestTests
{
    [Fact]
    public void LoadTestDoc_IncludesStagingChecklist()
    {
        var doc = File.ReadAllText(GetPath("docs/LOAD_TEST.md"));
        doc.Should().Contain("Staging environment checklist");
        doc.Should().Contain("API instances");
        doc.Should().Contain("Worker instances");
        doc.Should().Contain("pgvector");
    }

    [Fact]
    public void K6Horizon3_Script_HasReadPathThresholds()
    {
        var script = File.ReadAllText(GetPath("tests/load/k6-horizon3.js"));
        script.Should().Contain("K6_PROFILE");
        script.Should().Contain("endpoint:read");
        script.Should().Contain("p(95)<500");
    }

    [Fact]
    public void LoadTestSeeder_ProjectExists()
    {
        File.Exists(GetPath("tests/EnhancementHub.LoadTestSeeder/Program.cs")).Should().BeTrue();
        File.Exists(GetPath("tests/EnhancementHub.LoadTestSeeder/EnhancementHub.LoadTestSeeder.csproj")).Should().BeTrue();
    }

    [Fact]
    public void RepositoryIndexing_PreventsConcurrentDuplicates()
    {
        var executor = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Background/Executors/RepositoryIndexingJobExecutor.cs"));
        executor.Should().Contain("DisableConcurrentExecution");
    }

    [Fact]
    public void ApiListRequests_UsesPagination()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Api/Controllers/EnhancementRequestsController.cs"));
        controller.Should().Contain("pageSize = 25");
    }

    [Fact]
    public void LoadNightlyWorkflow_Exists()
    {
        File.Exists(GetPath(".github/workflows/load-nightly.yml")).Should().BeTrue();
    }

    [Fact]
    public void RunHorizon3Script_Exists()
    {
        File.Exists(GetPath("scripts/run-load-test-horizon3.mjs")).Should().BeTrue();
    }

    [Fact]
    public void Roadmap_MarksHorizon3Proven()
    {
        var roadmap = File.ReadAllText(GetPath("docs/ROADMAP.md"));
        roadmap.Should().Contain("Load test **proven**");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
