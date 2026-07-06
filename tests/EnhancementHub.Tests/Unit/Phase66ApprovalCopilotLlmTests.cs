using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase66ApprovalCopilotLlmTests
{
    [Fact]
    public void ApprovalCopilotService_UsesLlmWithHeuristicFallback()
    {
        var service = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Services/Approvals/ApprovalCopilotService.cs"));
        service.Should().Contain("IApprovalCopilotService");
        service.Should().Contain("FeatureFlags.ApprovalCopilot");
        service.Should().Contain("AiWorkflowStep.ApprovalCopilot");
        service.Should().Contain("BuildHeuristic");
        service.Should().Contain("\"Llm\"");
        service.Should().Contain("HeuristicFallback");
    }

    [Fact]
    public void GetApprovalRecommendationQuery_DelegatesToApprovalCopilotService()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Approvals/Queries/GetApprovalRecommendationQuery.cs"));
        handler.Should().Contain("IApprovalCopilotService");
        handler.Should().NotContain("BuildSummary");
    }

    [Fact]
    public void ApprovalRecommendationDto_IncludesSourceField()
    {
        var dto = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Approvals/Dtos/ApprovalRecommendationDto.cs"));
        dto.Should().Contain("string Source");
    }

    [Fact]
    public void Infrastructure_RegistersApprovalCopilotService()
    {
        var extensions = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs"));
        extensions.Should().Contain("IApprovalCopilotService, ApprovalCopilotService");
    }

    [Fact]
    public void FeatureFlag_ApprovalCopilot_IsDefined()
    {
        var features = File.ReadAllText(GetPath("src/EnhancementHub.Application/Abstractions/IFeatureService.cs"));
        features.Should().Contain("ApprovalCopilot");
    }

    [Fact]
    public void Appsettings_EnablesApprovalCopilotFeature()
    {
        var settings = File.ReadAllText(GetPath("src/EnhancementHub.Web/appsettings.json"));
        settings.Should().Contain("\"ApprovalCopilot\": true");
    }

    [Fact]
    public void ApprovalQueueApp_ShowsRecommendationSource()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApprovalQueueApp.tsx"));
        page.Should().Contain("recommendationSourceLabel");
        page.Should().Contain("recommendation.source");
        page.Should().Contain("AI copilot");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
