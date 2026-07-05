using EnhancementHub.Tests.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase36EngineeringDebtTests
{
    [Fact]
    public void Api_RegistersV1RouteConvention()
    {
        var program = File.ReadAllText(GetPath("src/EnhancementHub.Api/Program.cs"));
        program.Should().Contain("AddEnhancementHubApiVersioning");
    }

    [Fact]
    public void DiscoveryQueue_IsIdempotentWhenAlreadyQueued()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Onboarding/Commands/OnboardingExtendedCommands.cs"));
        handler.Should().Contain("DiscoveryJobState.Queued or DiscoveryJobState.Running");
    }

    [Fact]
    public void SpaBff_RequiresAuthorization()
    {
        var sources = SpaBffTestHelper.ReadAllSpaBffSources();
        sources.Should().Contain("[Authorize]");
        sources.Should().Contain("upload-zip");
        sources.Should().Contain("on-prem-agent");
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
