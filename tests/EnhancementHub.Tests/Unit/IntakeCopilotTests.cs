using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class IntakeCopilotTests
{
    [Fact]
    public void IntakeCopilotRoadmap_Exists()
    {
        var path = Path.Combine(GetRepoRoot(), "docs/INTAKE_COPILOT_ROADMAP.md");
        File.Exists(path).Should().BeTrue();
        var content = File.ReadAllText(path);
        content.Should().Contain("Intake Copilot");
        content.Should().Contain("Quick draft");
        content.Should().Contain("Deep grounding");
    }

    [Fact]
    public void SpaIntakeBff_ExposesSessionEndpoints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaIntakeController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("web-api/spa/intake");
        content.Should().Contain("StartIntakeCopilotSessionCommand");
        content.Should().Contain("SendIntakeCopilotMessageCommand");
    }

    [Fact]
    public void CreateRequestApp_IncludesIntakeCopilotPanel()
    {
        var app = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        app.Should().Contain("IntakeCopilotPanel");
        app.Should().Contain("applyCopilotDraft");
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
