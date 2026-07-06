using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase56DesignPartnerTests
{
    [Fact]
    public void DesignPartnerPlaybook_IncludesSixWeekCadenceAndMetrics()
    {
        var doc = File.ReadAllText(GetPath("docs/DESIGN_PARTNER_PLAYBOOK.md"));
        doc.Should().Contain("6-week");
        doc.Should().Contain("Success metrics");
        doc.Should().Contain("Time to analysis");
        doc.Should().Contain("Pilot NPS");
    }

    [Fact]
    public void CaseStudyTemplate_ExistsForAnonymizedPartner()
    {
        var doc = File.ReadAllText(GetPath("docs/CASE_STUDY_TEMPLATE.md"));
        doc.Should().Contain("Anonymized");
        doc.Should().Contain("Architect hours per request");
        doc.Should().Contain("Pilot NPS");
    }

    [Fact]
    public void SpaFeedbackController_ExposesSubmitEndpoint()
    {
        var controller = File.ReadAllText(GetPath("src/EnhancementHub.Web/Controllers/Spa/SpaFeedbackController.cs"));
        controller.Should().Contain("web-api/spa/feedback");
        controller.Should().Contain("SubmitProductFeedbackCommand");
        controller.Should().Contain("NpsScore");
    }

    [Fact]
    public void ProductFeedbackDomain_IncludesEntityAndWorkflowKey()
    {
        var entity = File.ReadAllText(GetPath("src/EnhancementHub.Domain/Entities/ProductFeedback.cs"));
        entity.Should().Contain("WorkflowKey");
        entity.Should().Contain("NpsScore");
        entity.Should().Contain("Comment");
    }

    [Fact]
    public void FeedbackWidget_MountedInSpaShell()
    {
        var shell = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/SpaShell.tsx"));
        shell.Should().Contain("FeedbackWidget");
        var widget = File.ReadAllText(GetPath("src/EnhancementHub.Web/ClientApp/src/components/FeedbackWidget.tsx"));
        widget.Should().Contain("submitProductFeedback");
        widget.Should().Contain("NPS");
    }

    [Fact]
    public void RoiReport_IncludesPilotMetrics()
    {
        var dto = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Reporting/Dtos/RoiReportDto.cs"));
        dto.Should().Contain("AverageTimeToAnalysisHours");
        dto.Should().Contain("AverageTimeToApprovalHours");
        dto.Should().Contain("MockAiRunPercent");
        dto.Should().Contain("AveragePilotNps");

        var query = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Reporting/Queries/GetRoiReportQuery.cs"));
        query.Should().Contain("ProductFeedbacks");
        query.Should().Contain("mock");
    }

    [Fact]
    public void RoiAdminPage_ShowsPilotMetricCards()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Roi.cshtml"));
        page.Should().Contain("Avg time to analysis");
        page.Should().Contain("Mock AI runs");
        page.Should().Contain("Pilot NPS");
    }

    [Fact]
    public void ProductScorecard_IncludesPilotMeasuredValues()
    {
        var scorecard = File.ReadAllText(GetPath("docs/PRODUCT_SCORECARD.md"));
        scorecard.Should().Contain("Phase 56");
        scorecard.Should().Contain("Measured");
        scorecard.ToLowerInvariant().Should().Contain("load test **proven**");
    }

    [Fact]
    public void Phase56Migration_Exists()
    {
        Directory.GetFiles(GetPath("src/EnhancementHub.Infrastructure/Migrations"), "*Phase56*")
            .Should().NotBeEmpty();
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}
