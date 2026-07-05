using EnhancementHub.Application.Common;
using EnhancementHub.Domain.Entities;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class FinOpsPhaseATests
{
    [Fact]
    public void ApplicationAnalysisContextFormatter_IncludesDeploymentNotesAndProfileFields()
    {
        var profile = new ApplicationProfile
        {
            KeyComponents = "{\"Name\":\"OrdersController\"}",
            DatabaseUsage = "EnhancementHubDbContext",
            ExternalIntegrations = "OrdersController",
            DeploymentNotes = "Azure Functions allowed for async only"
        };

        var result = ApplicationAnalysisContextFormatter.Format(
            profile,
            "Azure App Service P1v3; avoid new SaaS");

        result.Should().Contain("Deployment & infrastructure constraints");
        result.Should().Contain("Azure Functions allowed");
        result.Should().Contain("Key components");
        result.Should().Contain("Database usage");
    }

    [Fact]
    public void ApplicationAnalysisContextFormatter_UsesApplicationNotesWhenProfileMissing()
    {
        var result = ApplicationAnalysisContextFormatter.Format(
            profile: null,
            deploymentNotes: "On-prem VMs only; no public cloud");

        result.Should().Be("Deployment & infrastructure constraints:\nOn-prem VMs only; no public cloud");
    }

    [Fact]
    public void PromptSanitizer_IncludesApplicationContextSection()
    {
        var sanitizer = new Infrastructure.Services.PromptSanitizer();
        var prompt = sanitizer.BuildStructuredPrompt(
            "Add webhook",
            "Capture cancellations",
            repositoryContext: null,
            applicationContext: "Azure App Service; prefer Worker over Functions");

        prompt.Should().Contain("Application & infrastructure context:");
        prompt.Should().Contain("Azure App Service");
    }

    [Fact]
    public void OpenAiAnalysisService_PromptMentionsInfrastructureConstraints()
    {
        var path = Path.Combine(GetRepoRoot(), "src/EnhancementHub.Infrastructure/Services/OpenAiAnalysisService.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("applicationContext");
        content.Should().Contain("lower-cost alternative");
    }

    [Fact]
    public void OnboardingWizard_CapturesDeploymentNotes()
    {
        var app = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src/EnhancementHub.Web/ClientApp/src/apps/OnboardingWizardApp.tsx"));
        app.Should().Contain("deploymentNotes");
        app.Should().Contain("infrastructure notes");
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
