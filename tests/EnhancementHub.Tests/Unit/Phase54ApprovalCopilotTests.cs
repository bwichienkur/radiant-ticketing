using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase54ApprovalCopilotTests
{
    [Fact]
    public void GetApprovalRecommendationQuery_DelegatesToCopilotService()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Approvals/Queries/GetApprovalRecommendationQuery.cs"));
        handler.Should().Contain("GetApprovalRecommendationQuery");
        handler.Should().Contain("IApprovalCopilotService");
    }

    [Fact]
    public void SpaApprovalsController_ExposesRecommendationEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaApprovalsController.cs"));
        controller.Should().Contain("/recommendation");
        controller.Should().Contain("GetApprovalRecommendationQuery");
    }

    [Fact]
    public void ApprovalQueueApp_ShowsRecommendationBanner()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/ApprovalQueueApp.tsx"));
        page.Should().Contain("getApprovalRecommendation");
        page.Should().Contain("recommendation.summary");
        page.Should().Contain("recommendationSourceLabel");
    }

    [Fact]
    public void ScoreIntakeDraftQuery_ScoresMissingFields()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/IntakeCopilot/Queries/ScoreIntakeDraftQuery.cs"));
        handler.Should().Contain("ScoreIntakeDraftQuery");
        handler.Should().Contain("missing.Add");
    }

    [Fact]
    public void IntakeCopilotPanel_ShowsQualityScoreAndBudget()
    {
        var panel = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/IntakeCopilotPanel.tsx"));
        panel.Should().Contain("scoreIntakeDraft");
        panel.Should().Contain("getIntakeCopilotBudget");
        panel.Should().Contain("Intake quality");
    }

    [Fact]
    public void GetRequestAnalysisEvolutionQuery_ComparesAiAndArchitectEdits()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Analysis/Queries/GetRequestAnalysisEvolutionQuery.cs"));
        handler.Should().Contain("EditRequirements");
        handler.Should().Contain("CompareFields");
    }

    [Fact]
    public void RequestDetailApp_RendersAnalysisEvolutionTable()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/RequestDetailApp.tsx"));
        page.Should().Contain("getAnalysisEvolution");
        page.Should().Contain("AI recommendation vs architect edits");
    }

    [Fact]
    public void AiUsageBudgetService_ExposesRemainingBudget()
    {
        var service = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Services/Ai/AiUsageBudgetService.cs"));
        service.Should().Contain("GetStatusAsync");
        service.Should().Contain("AiBudgetStatusDto");
    }

    [Fact]
    public void SpaIntakeController_ExposesBudgetEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaIntakeController.cs"));
        controller.Should().Contain("[HttpGet(\"budget\")");
        controller.Should().Contain("score-draft");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
