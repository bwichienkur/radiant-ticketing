using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class IntakeProvenanceTests
{
    [Fact]
    public void CreateRequestFromIntakeSession_AcceptsFormOverrides()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Application/Features/IntakeCopilot/Commands/IntakeCopilotCommands.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("IntakeCopilotSubmitOverridesDto? Overrides");
        content.Should().Contain("Policy source:");
    }

    [Fact]
    public void IntakeCopilotService_EnrichesMockTurnWithTemplateId()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Infrastructure/Services/IntakeCopilotService.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("EnrichTurnWithTemplateAsync");
        content.Should().Contain("ResolveTemplateIdAsync(category, cancellationToken)");
    }

    [Fact]
    public void PolicyIntakeCommands_RedactsStoredPolicyText()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Application/Features/IntakeCopilot/Commands/PolicyIntakeCommands.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("IPiiRedactionService");
        content.Should().Contain("_piiRedaction.Redact");
    }

    [Fact]
    public void CreateRequestApp_UsesIntakeSessionSubmitWhenAvailable()
    {
        var app = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        app.Should().Contain("createRequestFromIntakeSession");
        app.Should().Contain("intakeSessionId");
        app.Should().Contain("onSessionChange={setIntakeSessionId}");
    }

    [Fact]
    public void SpaIntakeBff_CreateRequestAcceptsOverrides()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Web/Controllers/Spa/SpaIntakeController.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("SpaIntakeCreateRequestRequest");
        content.Should().Contain("request?.Overrides");
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
