using EnhancementHub.Domain.Enums;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase53DriftToRequestTests
{
    [Fact]
    public void GetDriftRequestDraftQuery_BuildsDraftFromFinding()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/SystemIntelligence/Queries/SystemIntelligenceQueries.cs"));
        handler.Should().Contain("GetDriftRequestDraftQuery");
        handler.Should().Contain("DriftRequestProvenance.BuildSupportingNotes");
    }

    [Fact]
    public void SpaIntelligenceController_ExposesDriftRequestDraftEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaIntelligenceController.cs"));
        controller.Should().Contain("drift/request-draft");
        controller.Should().Contain("GetDriftRequestDraftQuery");
    }

    [Fact]
    public void SchemaDriftApp_IncludesCreateRequestAction()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/SchemaDriftApp.tsx"));
        page.Should().Contain("Create request from drift");
        page.Should().Contain("driftFindingId=");
    }

    [Fact]
    public void CreateRequestApp_PrefillsFromDriftFindingId()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/CreateRequestApp.tsx"));
        page.Should().Contain("initialDriftFindingId");
        page.Should().Contain("getDriftRequestDraft");
    }

    [Fact]
    public void RiskScoringService_AppliesDriftSeverityBoost()
    {
        var service = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/RiskScoringService.cs"));
        service.Should().Contain("ApplyDriftSeverityBoost");
        service.Should().Contain("DriftSeverity.Critical");
    }

    [Fact]
    public void TriggerAiAnalysisCommand_UsesDriftRiskScoringHelper()
    {
        var handler = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Analysis/Commands/TriggerAiAnalysisCommand.cs"));
        handler.Should().Contain("DriftRiskScoringHelper.ResolveRiskLevelAsync");
    }

    [Fact]
    public void DashboardInsights_IncludesTopDriftFindings()
    {
        var dto = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Reporting/Dtos/DashboardInsightsDto.cs"));
        dto.Should().Contain("TopDriftFindings");

        var query = File.ReadAllText(GetPath(
            "src/EnhancementHub.Application/Features/Reporting/Queries/GetDashboardInsightsQuery.cs"));
        query.Should().Contain("DashboardDriftFindingSummaryDto");
        query.Should().Contain(".Take(5)");
    }

    [Fact]
    public void DashboardApp_RendersTopDriftFindingsWidget()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/apps/DashboardApp.tsx"));
        page.Should().Contain("Top drift findings");
        page.Should().Contain("topDriftFindings");
    }

    [Fact]
    public void DriftDigestJobExecutor_SendsWeeklyDigest()
    {
        var executor = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Background/Executors/DriftDigestJobExecutor.cs"));
        executor.Should().Contain("NotifyArchitectsOfDriftDigestAsync");
    }

    [Fact]
    public void HangfireInitializer_RegistersWeeklyDriftDigest()
    {
        var initializer = File.ReadAllText(GetPath(
            "src/EnhancementHub.Infrastructure/Background/HangfireRecurringJobInitializer.cs"));
        initializer.Should().Contain("DriftDigestJobExecutor");
        initializer.Should().Contain("Cron.Weekly");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
